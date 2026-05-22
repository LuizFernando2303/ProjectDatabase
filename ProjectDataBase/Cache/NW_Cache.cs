using Autodesk.Navisworks.Api;
using ProjectDataBase.Cache;
using ProjectDataBase.Library.Tree;
using ProjectDataBase.Library.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectDataBase.Config
{
    public static class NW_Cache
    {
        private static readonly string BasePath =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "ProjectDataBase",
                "Cache");

        private static readonly string LogFile =
            Path.Combine(BasePath, "cache_logs.log");

        private static readonly ConcurrentQueue<string> _logQueue =
            new ConcurrentQueue<string>();

        private static readonly AutoResetEvent _logSignal =
            new AutoResetEvent(false);

        private static bool _loggingStarted = false;

        private static string CurrentCacheFile;
        private static string CurrentPropsFile;

        private static string _currentProjectId;

        private const int CacheVersion = 3;
        private const int PropsVersion = 1;
        private const double EPS = 0.0001;
        private const int divX = 8;
        private const int divY = 8;
        private const int divZ = 4;

        private static bool _initialized = false;
        private static readonly object _lock = new object();

        private static Dictionary<Guid, NodeCache> Cache =
            new Dictionary<Guid, NodeCache>(100000);

        private static Dictionary<Guid, ElementProperty[]> PropertyCache =
            new Dictionary<Guid, ElementProperty[]>(50000);

        private static Dictionary<string, string> StringPool =
            new Dictionary<string, string>(10000);

        private static Dictionary<Chunk, List<Guid>> SpatialIndex =
            new Dictionary<Chunk, List<Guid>>(1024);

        public static ChunkCollection RootBoxes = new ChunkCollection();
        public static Search_Cache Search_Cache = new Search_Cache();

        public static int Count => Cache.Count;

        public static int Initialize()
        {
            var doc = Application.ActiveDocument;
            string projectId = GetProjectId(doc); 

            lock (_lock)
            {
                if (_initialized && _currentProjectId != projectId)
                {
                    Log($"Project changed: {_currentProjectId} -> {projectId}");
                    ResetCache(); 
                }

                if (_initialized && _currentProjectId == projectId)
                {
                    string message = $"Cache already initialized for project {projectId}. Current cache count: {Cache.Count}";
                    Log(message);
                    return Cache.Count;
                }

                Directory.CreateDirectory(BasePath);
                Log($"Initializing cache... Base path: {BasePath}");
                Log($"Project ID: {projectId}");

                CurrentCacheFile = Path.Combine(BasePath, $"cache_{projectId}.bin");
                CurrentPropsFile = Path.Combine(BasePath, $"cache_props_{projectId}.bin");
                Log($"Cache file: {CurrentCacheFile}");
                Log($"Properties file: {CurrentPropsFile}");

                var sw = Stopwatch.StartNew();

                CaptureRootBoxes();
                sw.Stop();
                Log($"Captured root boxes in {sw.Elapsed.TotalSeconds:F2} seconds");

                sw.Restart();
                int loaded = TryLoadCache();
                sw.Stop();
                Log($"Cache Loaded {loaded}");

                sw.Restart();
                TryLoadPropertiesCache();
                sw.Stop();
                Log($"Properties cache loaded {loaded}");

                if (loaded == 0)
                {
                    Log("Building cache from document...");

                    if (doc?.Models != null)
                    {
                        Cache.Clear();
                        PropertyCache.Clear();
                        SpatialIndex.Clear();

                        foreach (var model in doc.Models)
                        {
                            if (model?.RootItem == null)
                                continue;

                            Build(model.RootItem);
                        }

                        WriteCacheToFile();
                        loaded = Cache.Count;
                    }

                    Log($"Cache built with {loaded} items");
                }

                sw.Restart();
                //LoadSearch();
                sw.Stop();
                Log($"Search cache loaded in {sw.Elapsed.TotalSeconds:F2} seconds");

                _currentProjectId = projectId; 
                _initialized = true;
                
                string log = $"Cache initialized. Loaded {Cache.Count} items.";
                Log(log);

                return loaded;
            }
        }

        private static string GetProjectId(Document document)
        {
            try
            {
                var doc = document;
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
            catch (Exception ex)
            {
                Log(ex);
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
                    box.CreateSubChunks(divX, divY, divZ);
                    RootBoxes.AddRange(box.SubChunks.Cast<Chunk>());
                }
                catch (Exception ex)
                {
                    Log(ex);
                }
            }
        }

        private static void LoadSearch()
        {
            Guid[] items = GetElements();
            foreach (Guid guid in items)
            {
                NodeCache node = GetNode(guid);
                ElementProperty[] elementProperties = GetProperties(guid);
                Search_Cache.AddNode(guid, node, elementProperties);
            }
        }

        public static int Build(ModelItem root)
        {
            if (root == null)
                return 0;

            Guid rootGuid;

            var stack =
                new Stack<(ModelItem item, Guid parent)>(1024);

            stack.Push((root, Guid.Empty));

            int count = 0;

            while (stack.Count > 0)
            {
                var (current, parentId) = stack.Pop();

                if (current == null)
                    continue;

                Guid id;

                try
                {
                    id = Library.Identity
                        .IdentityFunctions
                        .GetNewGuid(current);
                    rootGuid = id;
                }
                catch (Exception ex)
                {
                    Log(ex);
                    continue;
                }

                NodeCache node;

                if (!Cache.TryGetValue(id, out node))
                    node = new NodeCache();

                node.Parent = parentId;

                node.Name = Pool(current.DisplayName);

                try
                {
                    var bb = current.BoundingBox();

                    if (bb != null)
                        node.Space =
                            CompactBox.From(
                                Box.CreateFromBoundingBox(bb));

                    if (node.Space.HasValue)
                    {
                        var box = node.Space.Value.ToBox();

                        foreach (Chunk chunk in RootBoxes)
                        {
                            if (!chunk.Intersects(box))
                                continue;

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
                catch (Exception ex)
                {
                    Log(ex);
                }

                try
                {
                    var props = ElementProperty.Make(current);

                    if (props != null && props.Length > 0)
                    {
                        for (int i = 0; i < props.Length; i++)
                        {
                            props[i].Name = Pool(props[i].Name);
                            props[i].Category = Pool(props[i].Category);
                            props[i].Value = Pool(props[i].Value);
                        }

                        PropertyCache[id] = props;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex);
                }

                var children = current.Children?.ToList();

                if (children != null && children.Count > 0)
                {
                    var ids = new List<Guid>(children.Count);

                    for (int i = 0; i < children.Count; i++)
                    {
                        var child = children[i];

                        if (child == null)
                            continue;

                        try
                        {
                            var cid =
                                Library.Identity
                                .IdentityFunctions
                                .GetNewGuid(child);

                            ids.Add(cid);

                            stack.Push((child, id));
                        }
                        catch (Exception ex)
                        {
                            Log(ex);
                        }
                    }

                    node.Children = ids.ToArray();
                }
                else
                {
                    node.Children = new Guid[0];
                }

                Cache[id] = node;

                // mark the root node
                if (id == rootGuid)
                    Cache[id].Parent = Guid.Empty;

                count++;
            }

            return count;
        }

        public static Guid[] GetElements()
        {
            return Cache.Keys.ToArray();
        }

        public static Guid[] GetChildren(Guid id)
        {
            var sw = Stopwatch.StartNew();

            NodeCache node;
            var result = Cache.TryGetValue(id, out node) ? node.Children : new Guid[0];

            sw.Stop();
            Log($"GetChildren({id}) returned {result.Length} items in {sw.ElapsedMilliseconds} ms");
            return result;
        }

        public static Guid GetParent(Guid id)
        {
            var sw = Stopwatch.StartNew();

            NodeCache node;
            var result = Cache.TryGetValue(id, out node) ? node.Parent : Guid.Empty;

            sw.Stop();
            Log($"GetParent({id}) returned {result} in {sw.ElapsedMilliseconds} ms");
            return result;
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

            var sw = Stopwatch.StartNew();

            var visited = new HashSet<Guid>();

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

                    if (!visited.Add(id))
                        continue;

                    NodeCache node;

                    if (!Cache.TryGetValue(id, out node))
                        continue;

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
                        sw.Stop();
                        Log($"FindByBox({box}) found {id} in {sw.ElapsedMilliseconds} ms");

                        return id;
                    }
                }
            }

            sw.Stop();
            Log($"FindByBox({box}) found nothing in {sw.ElapsedMilliseconds} ms");

            return Guid.Empty;
        }

        public static ElementProperty[] GetProperties(Guid id)
        {
            var sw = Stopwatch.StartNew();

            ElementProperty[] props;

            if (PropertyCache.TryGetValue(id, out props))
            {
                sw.Stop();
                //Log($"GetProperties({id}) returned {props.Length} items in {sw.ElapsedMilliseconds} ms");
                return props;
            }

            var item = GetModelItem(id);

            props = item != null
                ? ElementProperty.Make(item)
                : new ElementProperty[0];

            if (props != null && props.Length > 0)
            {
                for (int i = 0; i < props.Length; i++)
                {
                    props[i].Name = Pool(props[i].Name);
                    props[i].Category = Pool(props[i].Category);
                    props[i].Value = Pool(props[i].Value);
                }

                PropertyCache[id] = props;
            }

            sw.Stop();
            //Log($"GetProperties({id}) computed {props.Length} items in {sw.ElapsedMilliseconds} ms");

            return props ?? new ElementProperty[0];
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
            catch (Exception ex)
            {
                Log(ex);
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
            var swSearch = Stopwatch.StartNew();

            try
            {
                var result = TreeFunctions.GetModelItemByPath(BuildPath(id), id);
                swSearch.Stop();
                //Log($"GetModelItem: {id} found in {swSearch.ElapsedMilliseconds} ms");
                return result;
            }
            catch (Exception ex)
            {
                Log(ex);
                return null;
            }
        }

        public static ModelItemCollection GetModelItems(Guid[] ids)
        {
            var swSearch = Stopwatch.StartNew();

            try
            {
                var collection = new ModelItemCollection();
                for (int i = 0; i < ids.Length; i++)
                {
                    var item = GetModelItem(ids[i]);
                    if (item != null)
                        collection.Add(item);
                }

                swSearch.Stop();
                Log($"GetModelItems: {ids.Length} items found in {swSearch.ElapsedMilliseconds} ms");

                return collection;
            }
            catch (Exception ex)
            {
                Log(ex);
                return new ModelItemCollection();
            }
        }

        public static NodeCache GetNode(Guid guid)
        {
            NodeCache node;
            if (Cache.TryGetValue(guid, out node))
                return node;

            return new NodeCache();
        }

        public static Guid GetGuid(NodeCache node)
        {
            return Cache.FirstOrDefault(kv => kv.Value == node).Key;
        }

        public static NodeCache GetRootNode()
        {
            return Cache.Values.FirstOrDefault(n => n.Parent == Guid.Empty) ?? new NodeCache();
        }

        public static string BuildPath(Guid id)
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

            try
            {
                Document doc =
                    Autodesk.Navisworks.Api.Application.ActiveDocument;

                using (var stream = new FileStream(
                    CurrentCacheFile,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.ReadWrite,
                    1024 * 1024))
                using (var writer = new BinaryWriter(stream))
                {
                    writer.Write(CacheVersion);

                    writer.Write(doc.FileName);
                    writer.Write(File.GetLastWriteTimeUtc(doc.FileName).Ticks);
                    writer.Write(new FileInfo(doc.FileName).Length);

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

                            writer.Write(b.MinX);
                            writer.Write(b.MinY);
                            writer.Write(b.MinZ);

                            writer.Write(b.MaxX);
                            writer.Write(b.MaxY);
                            writer.Write(b.MaxZ);
                        }
                    }
                }

                WritePropertiesCache();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
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
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        public static int TryLoadCache()
        {
            if (!File.Exists(CurrentCacheFile))
                return 0;

            try
            {
                using (var stream = new FileStream(
                    CurrentCacheFile,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite,
                    1024 * 1024))
                using (var reader = new BinaryReader(stream))
                {
                    if (reader.ReadInt32() != CacheVersion)
                        return 0;

                    Document doc = Autodesk.Navisworks.Api.Application.ActiveDocument;

                    string cachedFile = reader.ReadString();
                    long cachedTicks = reader.ReadInt64();
                    long cachedLength = reader.ReadInt64();

                    if (!File.Exists(doc.FileName))
                        return 0;

                    if (!string.Equals(
                        cachedFile,
                        doc.FileName,
                        StringComparison.OrdinalIgnoreCase))
                        return 0;

                    if (File.GetLastWriteTimeUtc(doc.FileName).Ticks != cachedTicks)
                        return 0;

                    if (new FileInfo(doc.FileName).Length != cachedLength)
                        return 0;

                    Cache.Clear();
                    SpatialIndex.Clear();

                    int count = reader.ReadInt32();

                    for (int i = 0; i < count; i++)
                    {
                        var id = new Guid(reader.ReadBytes(16));

                        var node = new NodeCache();

                        node.Parent = new Guid(reader.ReadBytes(16));

                        node.Name = Pool(reader.ReadString());

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

                        if (node.Space.HasValue)
                        {
                            var box = node.Space.Value.ToBox();

                            foreach (Chunk chunk in RootBoxes)
                            {
                                if (!chunk.Intersects(box))
                                    continue;

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

                    return Cache.Count;
                }
            }
            catch (Exception ex)
            {
                Log(ex);
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

        private static void ResetCache()
        {
            lock (_lock)
            {
                Cache.Clear();
                PropertyCache.Clear();
                SpatialIndex.Clear();

                _initialized = false;

                CurrentCacheFile = null;
                CurrentPropsFile = null;

                Log("Cache reset");
            }
        }

        private static void StartLogger()
        {
            if (_loggingStarted)
                return;

            _loggingStarted = true;

            Task.Run(() =>
            {
                using (var stream = new FileStream(
                    LogFile,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite))
                using (var writer = new StreamWriter(stream))
                {
                    while (true)
                    {
                        _logSignal.WaitOne();

                        while (_logQueue.TryDequeue(out string msg))
                        {
                            writer.WriteLine(msg);
                        }

                        writer.Flush();
                    }
                }
            });
        }

        public static void Log(Exception ex)
        {
            StartLogger();

            _logQueue.Enqueue($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex} \n\n");

            _logSignal.Set();
        }

        public static void Log(string message)
        {
            Debug.WriteLine(message);

            StartLogger();

            _logQueue.Enqueue(
                DateTime.Now + " " + message + "\n\n");

            _logSignal.Set();
        }
    }
}