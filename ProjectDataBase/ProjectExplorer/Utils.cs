using ProjectDataBase.Config;
using ProjectDataBase.Library.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ProjectDataBase.ProjectExplorer
{
    public static class Utils
    {
        public static TreeNode CreateNodeChildrenProperties(TreeNode node)
        {
            if (node == null)
            {
                return null;
            }

            TreeNode baseNode = new TreeNode("Propriedades filhas");

            // category -> property -> values
            Dictionary<string, Dictionary<string, List<string>>> groupedProperties =
                new Dictionary<string, Dictionary<string, List<string>>>();

            foreach (TreeNode child in node.Nodes)
            {
                if (child.Tag is NodeCache childCache)
                {
                    var properties =
                        NW_Cache.GetProperties(
                            NW_Cache.GetGuid(childCache));

                    foreach (var property in properties)
                    {
                        if (string.IsNullOrWhiteSpace(property.Name))
                        {
                            continue;
                        }

                        string category =
                            string.IsNullOrWhiteSpace(property.Category)
                            ? "Sem categoria"
                            : property.Category;

                        if (!groupedProperties.ContainsKey(category))
                        {
                            groupedProperties[category] =
                                new Dictionary<string, List<string>>();
                        }

                        if (!groupedProperties[category].ContainsKey(property.Name))
                        {
                            groupedProperties[category][property.Name] =
                                new List<string>();
                        }

                        if (!string.IsNullOrWhiteSpace(property.Value))
                        {
                            groupedProperties[category][property.Name]
                                .Add(property.Value);
                        }
                    }
                }
            }

            // build tree
            foreach (var categoryKvp in groupedProperties.OrderBy(x => x.Key))
            {
                TreeNode categoryNode =
                    new TreeNode(categoryKvp.Key);

                foreach (var propertyKvp in categoryKvp.Value.OrderBy(x => x.Key))
                {
                    string propertyName = propertyKvp.Key;

                    string[] values =
                        propertyKvp.Value
                            .Distinct()
                            .OrderBy(v => v)
                            .ToArray();

                    TreeNode propertyNode =
                        new TreeNode($"{propertyName} ({values.Length})");

                    propertyNode.Tag = values;

                    foreach (string value in values)
                    {
                        propertyNode.Nodes.Add(new TreeNode(value));
                    }

                    categoryNode.Nodes.Add(propertyNode);
                }

                baseNode.Nodes.Add(categoryNode);
            }

            return baseNode;
        }
    }
}
