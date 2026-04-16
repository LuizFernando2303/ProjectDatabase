using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Navisworks.Api;
using ProjectDataBase.Config;
using System.Linq;
using System;
using ProjectDataBase.Library.Tree;

namespace ProjectDataBase
{
    [Plugin("ProjectDataBase", "LF", DisplayName = "Load cache", ToolTip = "")]
    public class LoadCache : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            var document = Application.MainDocument;

            if (document == null || !document.Models.Any())
                return 0;

            var root = document.Models.FirstOrDefault()?.RootItem;
            if (root == null)
                return 0;

            Library.Actions.Loader loader = new Library.Actions.Loader();

            int loaded = NW_Cache.Initialize();

            if (loaded == 0)
            {
                BuildCache(document);
                NW_Cache.WriteCacheToFile();
            }

            return 0;
        }

        private void BuildCache(Document document)
        {
            foreach (var model in document.Models)
            {
                if (model?.RootItem == null)
                    continue;

                NW_Cache.Build(model.RootItem);
            }
        }
    }

    [Plugin("ProjectDataBase.Search", "LF", DisplayName = "Search", ToolTip = "")]
    public class SearchPlugin : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            var document = Application.MainDocument;

            if (document == null || !document.Models.Any())
                return 0;

            // query via parâmetro (ou fallback)
            string query = "proof";

            Guid[] result = NW_Cache.Search_Cache.Search(query);

            if (result == null || result.Length == 0)
                return 0;

            var modelItems = new ModelItemCollection();

            for (int i = 0; i < result.Length; i++)
            {
                var item = ProjectDataBase.Config.NW_Cache.GetModelItem(result[i]);

                if (item != null)
                    modelItems.Add(item);
            }

            ProjectDataBase.Library.Tree.TreeFunctions.Isolate(modelItems);

            return 0;
        }
    }

    [Plugin("ProjectReport", "LF", DisplayName = "Relatorio de Projeto", ToolTip = "")]
    public class Report : Library.Plugins.ProjectReport.Loader { }

    [Plugin("___", "___", DisplayName = "___", ToolTip = "___")]
    public class Renderer : Library.Plugins.RenderObjects { }
}
