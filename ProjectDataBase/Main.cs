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
            var document = Application.MainDocument;

            if (document == null || !document.Models.Any())
                return 0;

            NW_Cache.Initialize();

            string query = "EXT";

            Guid[] result =
                NW_Cache.Search_Cache.Search(query);

            if (result == null || result.Length == 0)
                return 0;

            var modelItems =
                NW_Cache.GetModelItems(result);

            if (modelItems == null || modelItems.Count == 0)
                return 0;

            TreeFunctions.Isolate(modelItems);

            return 0;
        }
    }

    [Plugin("ProjectReport", "LF", DisplayName = "Relatorio de Projeto", ToolTip = "")]
    public class Report : Library.Plugins.ProjectReport.Loader { }

    [Plugin("___", "___", DisplayName = "___", ToolTip = "___")]
    public class Renderer : Library.Plugins.RenderObjects { }
}
