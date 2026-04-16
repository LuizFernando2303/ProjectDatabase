using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Plugins;
using ProjectDataBase.Config;
using ProjectDataBase.Library.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDataBase.Library.Plugins.ProjectReport
{
    public class Loader : AddInPlugin
    {
        public override int Execute(params string[] parameters)
        {
            LoadCache();
            //LoadUI();
            LoadReport();

            return 0;
        }

        private void LoadReport()
        {
            var document = Application.MainDocument;
            if (document == null || !document.Models.Any())
                return;

            var root = document.Models.FirstOrDefault()?.RootItem;
            if (root == null)
                return;

            Types.ProjectReport report = new Types.ProjectReport().MakeFromRoot(root);
            return;
        }

        private int LoadCache()
        {
            var document = Application.MainDocument;
            if (document == null || !document.Models.Any())
                return 0;

            var root = document.Models.FirstOrDefault()?.RootItem;
            if (root == null)
                return 0;

            NW_Cache.Initialize(root);

            int loaded = NW_Cache.TryLoadCache();

            if (loaded == 0)
            {
                foreach (var model in document.Models)
                {
                    if (model?.RootItem == null)
                        continue;

                    NW_Cache.Build(model.RootItem);
                }

                NW_Cache.WriteCacheToFile();
            }

            return loaded;
        }

        private void LoadUI()
        {
            // Thread separada (sem acesso ao cache por enquanto)
            var thread = new System.Threading.Thread(() =>
            {
                // futura UI
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
