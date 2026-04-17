using ProjectDataBase.Config;
using ProjectDataBase.Library.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ProjectDataBase.Cache
{
    public class Search_Cache
    {
        private static Dictionary<string, List<Guid>> TextIndex =
            new Dictionary<string, List<Guid>>(50000);

        public void AddNode(Guid id, NodeCache node, ElementProperty[] props = null)
        {
            if (node == null)
                return;

            IndexText(id, node.Name);

            var path = NW_Cache.BuildPath(id);
            IndexText(id, path);

            if (props != null)
            {
                for (int i = 0; i < props.Length; i++)
                {
                    var p = props[i];

                    IndexText(id, p.Name);
                    IndexText(id, p.Category);
                    IndexText(id, p.Value);
                }
            }
        }

        private static void IndexText(Guid id, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            foreach (var token in Tokenize(text))
            {
                List<Guid> list;

                if (!TextIndex.TryGetValue(token, out list))
                {
                    list = new List<Guid>(4);
                    TextIndex[token] = list;
                }

                if (list.Count == 0 || list[list.Count - 1] != id)
                {
                    if (!list.Contains(id))
                        list.Add(id);
                }
            }
        }

        public Guid[] Search(string query, int maxResults = 1000)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(query))
                return Empty(sw);

            var tokens = Tokenize(query).ToArray();
            if (tokens.Length == 0)
                return Empty(sw);

            var scores = new Dictionary<Guid, int>(1024);

            for (int i = 0; i < tokens.Length; i++)
            {
                List<Guid> list;

                if (!TextIndex.TryGetValue(tokens[i], out list))
                    continue;

                for (int j = 0; j < list.Count; j++)
                {
                    var id = list[j];

                    int s;
                    if (!scores.TryGetValue(id, out s))
                        scores[id] = 1;
                    else
                        scores[id] = s + 1;
                }
            }

            if (scores.Count == 0)
                return Empty(sw);

            var result = scores
                .OrderByDescending(kv => kv.Value)
                .Take(maxResults)
                .Select(kv => kv.Key)
                .ToArray();

            Log(sw, "Search");

            return result;
        }

        public static void Clear()
        {
            TextIndex.Clear();
        }

        private static IEnumerable<string> Tokenize(string input)
        {
            if (string.IsNullOrEmpty(input))
                yield break;

            var sb = new StringBuilder();

            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        yield return sb.ToString();
                        sb.Clear();
                    }
                }
            }

            if (sb.Length > 0)
                yield return sb.ToString();
        }

        private Guid[] Empty(Stopwatch sw)
        {
            Log(sw, "Search");
            return new Guid[0];
        }

        private void Log(Stopwatch sw, string name)
        {
            sw.Stop();

            var t = sw.Elapsed;

            string formatted =
                t.TotalSeconds >= 1 ? $"{t.TotalSeconds:F3}s" :
                t.TotalMilliseconds >= 1 ? $"{t.TotalMilliseconds:F2}ms" :
                $"{t.TotalMilliseconds * 1000:F2}µs";

            Debug.WriteLine($"{name}: {formatted}");
        }
    }
}