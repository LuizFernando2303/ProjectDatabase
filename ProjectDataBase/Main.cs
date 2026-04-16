using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Navisworks.Api;
using ProjectDataBase.Config;
using System.Linq;
using ProjectDataBase.Library.Types;
using System.IO;

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

            NW_Cache.Initialize(root);

            int loaded = NW_Cache.TryLoadCache();

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

    [Plugin("ProjectReport", "LF", DisplayName = "Relatorio de Projeto", ToolTip = "")]
    public class Report : Library.Plugins.ProjectReport.Loader { }

    [Plugin("___", "___", DisplayName = "___", ToolTip = "___")]
    public class Renderer : Library.Plugins.RenderObjects { }
}
