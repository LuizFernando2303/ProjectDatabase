using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Navisworks.Api;
using ProjectDataBase.Config;
using System.Linq;
using System;
using ProjectDataBase.Library.Tree;

namespace ProjectDataBase
{
    [Plugin("ProjectDataBase.Search", "LF", DisplayName = "Search", ToolTip = "")]
    public class SearchPlugin : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            var document = Application.MainDocument;

            if (document == null || !document.Models.Any())
                return 0;

            NW_Cache.Initialize();

            string query = "EXT";

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
