using Autodesk.Navisworks.Api;
using ProjectDataBase.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDataBase.Library.Types
{
    public struct ProjectElement
    {
        public string Name { get; private set; }
        public Guid Guid { get; private set; }
        public Guid Parent { get; private set; }
        public Guid[] Children { get; private set; }
        public Box Space { get; private set; }
        public Chunk Location { get; private set; }
        public ElementProperty[] Properties { get; private set; }

        public static ProjectElement Make(Guid element, Chunk chunk)
        {
            string name = null;
            ElementProperty[] properties = null;
            Guid[] children = null;
            Guid parent = Guid.Empty;
            Box space = null;

            System.Threading.Tasks.Parallel.Invoke(
                () => name = NW_Cache.GetName(element),
                () => properties = NW_Cache.GetProperties(element),
                () => children = NW_Cache.GetChildren(element),
                () => parent = NW_Cache.GetParent(element),
                () => space = NW_Cache.GetSpace(element)
            );

            if (string.IsNullOrEmpty(name) || properties == null || properties.Length == 0)
            {
                var modelItem = NW_Cache.GetModelItem(element);

                if (modelItem != null)
                {
                    if (string.IsNullOrEmpty(name))
                        name = modelItem.DisplayName;

                    if (properties == null || properties.Length == 0)
                        properties = ElementProperty.Make(modelItem);
                }
            }

            return new ProjectElement
            {
                Name = name ?? "",
                Guid = element,
                Parent = parent,
                Children = children ?? Array.Empty<Guid>(),
                Location = chunk,
                Space = space,
                Properties = properties ?? Array.Empty<ElementProperty>()
            };
        }
    }

    public struct ElementProperty
    {
        public string Name { get; private set; }
        public string Category { get; private set; }
        public string Value { get; private set; }

        public ElementProperty(string name, string category, string value)
        {
            Name = name;
            Category = category;
            Value = value;
        }

        public static ElementProperty[] Make(ModelItem modelItem)
        {
            List<ElementProperty> properties = new List<ElementProperty>();

            foreach (var dataProperty in modelItem.PropertyCategories)
            foreach (var data in dataProperty.Properties)
            {
                properties.Add(Make(data, dataProperty.DisplayName));
            }

            return properties.ToArray();
        }

        public static ElementProperty Make(DataProperty dataProperty, string category)
        {
            return new ElementProperty
            {
                Name = dataProperty.DisplayName,
                Category = category,
                Value = dataProperty.Value.IsDisplayString ? dataProperty.Value.ToDisplayString() : dataProperty.Value.ToString()
            };
        }
    }
}
