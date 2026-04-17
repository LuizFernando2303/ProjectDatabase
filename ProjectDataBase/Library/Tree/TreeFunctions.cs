using Autodesk.Navisworks.Api;
using Autodesk.Navisworks.Api.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Autodesk.Navisworks.Api.Interop.ComApiAutomation;
using ProjectDataBase.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

            if (pathParts.Length == 0)
                return null;

            // nível atual
            List<ModelItem> currentLevel = new List<ModelItem>();

            foreach (var model in document.Models)
            {
                if (model?.RootItem != null)
                    currentLevel.Add(model.RootItem);
            }

            // percorre nível a nível
            for (int depth = 0; depth < pathParts.Length; depth++)
            {
                string target = pathParts[depth];
                List<ModelItem> nextLevel = new List<ModelItem>(32);

                for (int i = 0; i < currentLevel.Count; i++)
                {
                    var item = currentLevel[i];
                    if (item == null) continue;

                    var name = item.DisplayName;
                    if (name == null) continue;

                    if (!string.Equals(Normalize(name), target, StringComparison.Ordinal))
                        continue;

                    // último nível → validar GUID
                    if (depth == pathParts.Length - 1)
                    {
                        try
                        {
                            var guid = Identity.IdentityFunctions.GetNewGuid(item);
                            if (guid == identity)
                                return item;
                        }
                        catch { }

                        continue;
                    }

                    // próximo nível
                    var children = item.Children;
                    if (children == null) continue;

                    foreach (var child in children)
                    {
                        if (child != null)
                            nextLevel.Add(child);
                    }
                }

                if (nextLevel.Count == 0)
                    return null;

                currentLevel = nextLevel;
            }

            return null;
        }

        [Obsolete]
        public static ModelItem _GetModelItemByPath(string path, Guid identity)
        {
            // start timer
            //Stopwatch sw = Stopwatch.StartNew();

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
                    {
                        //sw.Stop();

                        //var t = sw.Elapsed;

                        //Debug.WriteLine(
                        //    $"Model item found in: {t.TotalSeconds:F3}s " +
                        //    $"({t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3})"
                        //);

                        return current;
                    }

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
        /// Isola uma coleção de ModelItems na visualização do Navisworks.
        /// </summary>
        /// <remarks>
        /// Todos os outros elementos são ocultados, mantendo apenas os itens informados visíveis.
        /// Pode alterar a seleção atual e o estado da visualização do usuário.
        /// </remarks>
        /// <param name="coll">Coleção de itens a serem isolados.</param>
        /// <param name="selected">
        /// Se true, define os itens como seleção atual antes de isolar; caso contrário, mantém a seleção atual.
        /// </param>
        public static void Isolate(ModelItemCollection coll, bool selected = true)
        {
            if (coll == null || coll.Count == 0)
                return;

            if (selected)
            {
                Autodesk.Navisworks.Api.Application.ActiveDocument.CurrentSelection.Clear();
                Autodesk.Navisworks.Api.Application.ActiveDocument.CurrentSelection.AddRange(coll);
            }

            InwOpState10 state = ComApiBridge.State;
            InwOpSelection2 comSelection = (InwOpSelection2)ComApiBridge.ToInwOpSelection(coll);

            state.HiddenItemsResetAll();

            InwOpSelection2 inverseSelection = (InwOpSelection2)state.ObjectFactory(
                nwEObjectType.eObjectType_nwOpSelection, null, null);

            inverseSelection.SelectAll();
            inverseSelection.SubtractContents(comSelection);

            state.set_SelectionHidden(inverseSelection, true);

            state.ZoomInCurViewOnSel(comSelection);
        }

        private static string Normalize(string value)
        {
            return value?.Trim().ToLowerInvariant() ?? "";
        }
    }
}
