using Autodesk.Navisworks.Api;
using ProjectDataBase.Config;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace ProjectDataBase.Library.Types
{
    public class ProjectReport
    {
        public string ProjectName { get; private set; }
        public int TotalElements { get; private set; } = 0;
        public Box RootBox { get; private set; }
        public List<ProjectElement> Elements { get; private set; } = new List<ProjectElement>();

        public ProjectReport MakeFromRoot(ModelItem root)
        {
            string projectName = Application.MainDocument?.FileName ?? "Unknown";

            Box rootBox = Box.CreateFromModelItem(root);

            var elements = NW_Cache.GetElements();
            int total = elements.Length;
            int current = 0;

            var progress = Application.BeginProgress("Processing elements...");
            foreach (var guidElement in elements)
            {
                Box elementBox = NW_Cache.GetSpace(guidElement);
                if (elementBox == null)
                    continue;

                Chunk chunk = NW_Cache.RootBoxes.FindLargestIntersection(elementBox);

                ProjectElement element = ProjectElement.Make(guidElement, chunk);
                Elements.Add(element);

                TotalElements++;
                current++;

                double percent = current / (double)total;
                progress.Update(percent);
            }

            progress.Update(1.0);
            Application.EndProgress();

            ProjectName = projectName;
            RootBox = rootBox;

            return this;
        }
    }
}
