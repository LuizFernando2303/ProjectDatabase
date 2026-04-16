using Autodesk.Navisworks.Api;
using ProjectDataBase.Library.Tree;
using ProjectDataBase.Library.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ProjectDataBase.Config
{
    public static class NW_Cache
    {
        private static readonly string BasePath =
            @"C:\Program Files\Autodesk\Navisworks Manage 2026\Plugins\ProjectDataBase\Cache";

        private static string CurrentCacheFile;
        private static string CurrentPropsFile;

        private const int CacheVersion = 2;
        private const int PropsVersion = 1;
        private const double EPS = 0.0001;

        private static Dictionary<Guid, NodeCache> Cache =
            new Dictionary<Guid, NodeCache>(100000);

        private static Dictionary<Guid, ElementProperty[]> PropertyCache =
            new Dictionary<Guid, ElementProperty[]>(50000);

        private static Dictionary<string, string> StringPool =
            new Dictionary<string, string>(10000);

        private static Dictionary<Chunk, List<Guid>> SpatialIndex =
            new Dictionary<Chunk, List<Guid>>(1024);

        public static ChunkCollection RootBoxes = new ChunkCollection();

        public static int Count => Cache.Count;

        public static void Initialize(ModelItem root)
        {
            Directory.CreateDirectory(BasePath);

            string projectId = GetProjectId();

            CurrentCacheFile = Path.Combine(BasePath, $"cache_{projectId}.bin");
            CurrentPropsFile = Path.Combine(BasePath, $"cache_props_{projectId}.bin");

            CaptureRootBoxes();
            TryLoadCache();
            //TryLoadPropertiesCache();
        }

        private static string GetProjectId()
        {
            try
            {
                var doc = Application.MainDocument;
                string source = doc?.FileName ?? doc?.Title ?? "";

                if (string.IsNullOrEmpty(source))
                    return Guid.NewGuid().ToString("N");

                using (var md5 = MD5.Create())
                {
                    var bytes = Encoding.UTF8.GetBytes(source);
                    var hash = md5.ComputeHash(bytes);
                    return BitConverter.ToString(hash).Replace("-", "");
                }
            }
            catch
            {
                return Guid.NewGuid().ToString("N");
            }
        }

        private static string Pool(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";

            string existing;
            if (StringPool.TryGetValue(s, out existing))
                return existing;

            StringPool[s] = s;
            return s;
        }

        private static void CaptureRootBoxes()
        {
            RootBoxes.Clear();

            var doc = Application.MainDocument;
            if (doc?.Models == null) return;

            foreach (var model in doc.Models)
            {
                var root = model?.RootItem;
                if (root == null) continue;

                try
                {
                    var bb = root.BoundingBox();
                    if (bb == null) continue;

                    var box = Box.CreateFromBoundingBox(bb);
                    box.CreateSubChunks(4, 4);
                    RootBoxes.AddRange(box.SubChunks.Cast<Chunk>());
                }
                catch { }
            }
        }

        public static int Build(ModelItem root)
        {
            if (root == null) return 0;

            Cache.Clear();
            PropertyCache.Clear();
            SpatialIndex.Clear();

            var stack = new Stack<ModelItem>(1024);
            stack.Push(root);

            int count = 0;

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                if (current == null) continue;

                Guid id;
                try
                {
                    id = Library.Identity.IdentityFunctions.GetNewGuid(current);
                }
                catch
                {
                    continue;
                }

                var node = new NodeCache();
                node.Name = Pool(Normalize(current.DisplayName));

                try
                {
                    var bb = current.BoundingBox();
                    if (bb != null)
                        node.Space = CompactBox.From(Box.CreateFromBoundingBox(bb));

                    if (node.Space.HasValue)
                    {
                        var box = node.Space.Value.ToBox();

                        foreach (Chunk chunk in RootBoxes)
                        {
                            if (!chunk.Intersects(box)) continue;

                            List<Guid> list;
                            if (!SpatialIndex.TryGetValue(chunk, out list))
                            {
                                list = new List<Guid>(32);
                                SpatialIndex[chunk] = list;
                            }

                            list.Add(id);
                        }
                    }
                }
                catch { }

                try
                {
                    var props = ElementProperty.Make(current);
                    if (props != null && props.Length > 0)
                        PropertyCache[id] = props;
                }
                catch { }

                var children = current.Children?.ToList();

                if (children != null && children.Count > 0)
                {
                    var ids = new Guid[children.Count];

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];
                        if (child == null) continue;

                        try
                        {
                            var cid = Library.Identity.IdentityFunctions.GetNewGuid(child);
                            ids[i] = cid;
                            stack.Push(child);
                        }
                        catch { }
                    }

                    node.Children = ids;
                }
                else
                {
                    node.Children = new Guid[0];
                }

                Cache[id] = node;

                foreach (var cid in node.Children)
                {
                    if (cid == Guid.Empty) continue;

                    NodeCache childNode;
                    if (!Cache.TryGetValue(cid, out childNode))
                        childNode = new NodeCache();

                    childNode.Parent = id;
                    Cache[cid] = childNode;
                }

                count++;
            }

            return count;
        }

        public static Guid[] GetElements()
        {
            var arr = new Guid[Cache.Count];
            int i = 0;

            foreach (var k in Cache.Keys)
                arr[i++] = k;

            return arr;
        }

        public static Guid[] GetChildren(Guid id)
        {
            NodeCache node;
            return Cache.TryGetValue(id, out node) ? node.Children : new Guid[0];
        }

        public static Guid GetParent(Guid id)
        {
            NodeCache node;
            return Cache.TryGetValue(id, out node) ? node.Parent : Guid.Empty;
        }

        public static string GetName(Guid id)
        {
            NodeCache node;
            return Cache.TryGetValue(id, out node) ? node.Name : null;
        }

        public static Box GetSpace(Guid id)
        {
            NodeCache node;

            if (Cache.TryGetValue(id, out node) && node.Space.HasValue)
                return node.Space.Value.ToBox();

            return null;
        }

        public static Guid FindByBox(Box box)
        {
            if (box == null)
                return Guid.Empty;

            foreach (var chunk in RootBoxes)
            {
                if (!chunk.Intersects(box))
                    continue;

                List<Guid> list;
                if (!SpatialIndex.TryGetValue(chunk, out list))
                    continue;

                for (int i = 0; i < list.Count; i++)
                {
                    var id = list[i];
                    var node = Cache[id];

                    if (!node.Space.HasValue)
                        continue;

                    var b = node.Space.Value;

                    if (Equal(b.MinX, box.Min.X) &&
                        Equal(b.MinY, box.Min.Y) &&
                        Equal(b.MinZ, box.Min.Z) &&
                        Equal(b.MaxX, box.Max.X) &&
                        Equal(b.MaxY, box.Max.Y) &&
                        Equal(b.MaxZ, box.Max.Z))
                    {
                        return id;
                    }
                }
            }

            return Guid.Empty;
        }

        public static ElementProperty[] GetProperties(Guid id)
        {
            ElementProperty[] props;

            if (PropertyCache.TryGetValue(id, out props))
                return props;

            var item = GetModelItem(id);

            props = item != null
                ? ElementProperty.Make(item)
                : new ElementProperty[0];

            PropertyCache[id] = props;

            return props;
        }

        public static bool TryGetProfile(ModelItem item, out CacheProfile profile)
        {
            profile = default(CacheProfile);

            if (item == null)
                return false;

            Guid id;

            try
            {
                id = Library.Identity.IdentityFunctions.GetNewGuid(item);
            }
            catch
            {
                return false;
            }

            NodeCache node;
            if (!Cache.TryGetValue(id, out node))
                return false;

            Box box = null;
            if (node.Space.HasValue)
                box = node.Space.Value.ToBox();

            profile = CacheProfile.Set(
                id,
                node.Children ?? new Guid[0],
                box,
                BuildPath(id)
            );

            return true;
        }

        public static ModelItem GetModelItem(Guid id)
        {
            try
            {
                return TreeFunctions.GetModelItemByPath(BuildPath(id), id);
            }
            catch
            {
                return null;
            }
        }

        private static string BuildPath(Guid id)
        {
            var stack = new Stack<string>();

            while (id != Guid.Empty)
            {
                NodeCache node;
                if (!Cache.TryGetValue(id, out node))
                    break;

                stack.Push(node.Name);
                id = node.Parent;
            }

            return string.Join("/", stack.ToArray());
        }

        public static void WriteCacheToFile()
        {
            if (string.IsNullOrEmpty(CurrentCacheFile))
                return;

            using (var stream = new FileStream(CurrentCacheFile, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(CacheVersion);
                writer.Write(Cache.Count);

                foreach (var kv in Cache)
                {
                    writer.Write(kv.Key.ToByteArray());

                    var n = kv.Value;

                    writer.Write(n.Parent.ToByteArray());
                    writer.Write(n.Name ?? "");

                    writer.Write(n.Children.Length);
                    foreach (var c in n.Children)
                        writer.Write(c.ToByteArray());

                    writer.Write(n.Space.HasValue);
                    if (n.Space.HasValue)
                    {
                        var b = n.Space.Value;
                        writer.Write(b.MinX); writer.Write(b.MinY); writer.Write(b.MinZ);
                        writer.Write(b.MaxX); writer.Write(b.MaxY); writer.Write(b.MaxZ);
                    }
                }
            }

            WritePropertiesCache();
        }

        private static void WritePropertiesCache()
        {
            using (var stream = new FileStream(CurrentPropsFile, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(PropsVersion);
                writer.Write(PropertyCache.Count);

                foreach (var kv in PropertyCache)
                {
                    writer.Write(kv.Key.ToByteArray());

                    var props = kv.Value ?? new ElementProperty[0];
                    writer.Write(props.Length);

                    foreach (var p in props)
                    {
                        writer.Write(p.Name ?? "");
                        writer.Write(p.Category ?? "");
                        writer.Write(p.Value ?? "");
                    }
                }
            }
        }

        private static void TryLoadPropertiesCache()
        {
            if (!File.Exists(CurrentPropsFile))
                return;

            try
            {
                using (var stream = new FileStream(CurrentPropsFile, FileMode.Open))
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != PropsVersion)
                        return;

                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var id = new Guid(reader.ReadBytes(16));

                        int len = reader.ReadInt32();
                        var props = new ElementProperty[len];

                        for (int j = 0; j < len; j++)
                        {
                            props[j] = new ElementProperty(
                                reader.ReadString(),
                                reader.ReadString(),
                                reader.ReadString()
                            );
                        }

                        PropertyCache[id] = props;
                    }
                }
            }
            catch { }
        }

        public static int TryLoadCache()
        {
            if (!File.Exists(CurrentCacheFile))
                return 0;

            try
            {
                using (var stream = new FileStream(CurrentCacheFile, FileMode.Open))
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != CacheVersion)
                        return 0;

                    Cache.Clear();

                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var id = new Guid(reader.ReadBytes(16));

                        var node = new NodeCache();
                        node.Parent = new Guid(reader.ReadBytes(16));
                        node.Name = reader.ReadString();

                        int len = reader.ReadInt32();
                        node.Children = new Guid[len];

                        for (int j = 0; j < len; j++)
                            node.Children[j] = new Guid(reader.ReadBytes(16));

                        if (reader.ReadBoolean())
                        {
                            node.Space = new CompactBox
                            {
                                MinX = reader.ReadSingle(),
                                MinY = reader.ReadSingle(),
                                MinZ = reader.ReadSingle(),
                                MaxX = reader.ReadSingle(),
                                MaxY = reader.ReadSingle(),
                                MaxZ = reader.ReadSingle()
                            };
                        }

                        Cache[id] = node;
                    }

                    return Cache.Count;
                }
            }
            catch
            {
                return 0;
            }
        }

        private static string Normalize(string value)
        {
            return value == null ? "" : value.Trim().ToLowerInvariant();
        }

        private static bool Equal(double a, double b)
        {
            return Math.Abs(a - b) < EPS;
        }
    }
}