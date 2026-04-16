using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.Interop.ComApiAutomation;
using ProjectDataBase.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDataBase.Library.Tree
{
    public static class TreeFunctions
    {
        /// <summary>
        /// Traverses a hierarchy starting from the specified root identifier and returns a list of identifiers that
        /// satisfy the provided filter.
        /// </summary>
        /// <param name="rootId">The identifier of the root node to start traversal from.</param>
        /// <param name="filter">A function to determine whether a node should be included in the result.</param>
        /// <returns>A list of identifiers that match the filter criteria.</returns>
        public static List<Guid> Traverse(Guid rootId, Func<Guid, bool> filter)
        {
            var result = new List<Guid>();
            var stack = new Stack<Guid>();
            stack.Push(rootId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();

                if (filter(currentId))
                    result.Add(currentId);

                var children = NW_Cache.GetChildren(currentId);

                foreach (var child in children)
                    stack.Push(child);
            }

            return result;
        }

        /// <summary>
        /// Retrieves a ModelItem from the active document by matching a hierarchical path and identity GUID.
        /// </summary>
        /// <param name="path">The hierarchical path to the ModelItem, with segments separated by '/'.</param>
        /// <param name="identity">The GUID representing the identity of the ModelItem to locate.</param>
        /// <returns>The matching ModelItem if found; otherwise, null.</returns>
        public static ModelItem GetModelItemByPath(string path, Guid identity)
        {
            var document = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (document == null)
                return null;

            var pathParts = path.Split('/')
                                .Select(Normalize)
                                .ToArray();

            var stack = new Stack<(ModelItem item, int depth)>();

            foreach (var model in document.Models)
                stack.Push((model.RootItem, 0));

            while (stack.Count > 0)
            {
                var (current, depth) = stack.Pop();

                if (Normalize(current.DisplayName) != pathParts[depth])
                    continue;

                if (depth == pathParts.Length - 1)
                {
                    var guid = Identity.IdentityFunctions.GetNewGuid(current);

                    if (guid == identity)
                        return current;

                    continue;
                }

                foreach (var child in current.Children)
                {
                    stack.Push((child, depth + 1));
                }
            }

            return null;
        }

        /// <summary>
        /// Normalizes a string by trimming whitespace and converting to lower case. If the input is null, it returns an empty string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string Normalize(string value)
        {
            return value?.Trim().ToLowerInvariant() ?? "";
        }
    }
}
