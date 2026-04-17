using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Plugins;
using ProjectDataBase.Config;
using ProjectDataBase.Library.Tree;
using System;
using System.Diagnostics;
using System.Linq;

namespace ProjectDataBase
{
    [Plugin("ProjectDataBase.Search", "LF", DisplayName = "Search", ToolTip = "")]
    public class SearchPlugin : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            var total = Stopwatch.StartNew();

            var document = Application.MainDocument;

            if (document == null || !document.Models.Any())
                return 0;

            var swInit = Stopwatch.StartNew();
            NW_Cache.Initialize();
            swInit.Stop();

            string query = "EXT";

            var swSearch = Stopwatch.StartNew();
            Guid[] result = NW_Cache.Search_Cache.Search(query);
            swSearch.Stop();

            if (result == null || result.Length == 0)
            {
                total.Stop();
                Log("TOTAL", total);
                return 0;
            }

            var swResolve = Stopwatch.StartNew();

            var modelItems = new ModelItemCollection();

            for (int i = 0; i < result.Length; i++)
            {
                var item = NW_Cache.GetModelItem(result[i]);

                if (item != null)
                    modelItems.Add(item);
            }

            swResolve.Stop();

            var swIsolate = Stopwatch.StartNew();
            TreeFunctions.Isolate(modelItems);
            swIsolate.Stop();

            total.Stop();

            Debug.WriteLine("----- SEARCH PROFILING -----");
            Log("Initialize", swInit);
            Log("Search", swSearch);
            Log("Resolve (Guid → ModelItem)", swResolve);
            Log("Isolate", swIsolate);
            Log("TOTAL", total);

            return 0;
        }

        private void Log(string name, Stopwatch sw)
        {
            var t = sw.Elapsed;

            string formatted =
                t.TotalSeconds >= 1 ? $"{t.TotalSeconds:F3}s" :
                t.TotalMilliseconds >= 1 ? $"{t.TotalMilliseconds:F2}ms" :
                $"{t.TotalMilliseconds * 1000:F2}µs";

            Debug.WriteLine($"{name}: {formatted}");
        }
    }

    [Plugin("ProjectReport", "LF", DisplayName = "Relatorio de Projeto", ToolTip = "")]
    public class Report : Library.Plugins.ProjectReport.Loader { }

    [Plugin("___", "___", DisplayName = "___", ToolTip = "___")]
    public class Renderer : Library.Plugins.RenderObjects { }
}
