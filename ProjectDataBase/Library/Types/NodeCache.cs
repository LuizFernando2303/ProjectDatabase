using ProjectDataBase.Config;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ProjectDataBase.Library.Types
{
    public class NodeCache
    {
        public Guid Parent;
        public Guid[] Children;
        public CompactBox? Space;
        public string Name;

        public TreeNode ConvertToTreeNode()
        {
            string name = Name;

            if (name == "")
                name = NW_Cache.GetGuid(this).ToString();

            var node = new TreeNode(name)
            {
                Tag = this
            };

            foreach (var child in Children)
            {
                NodeCache ChildNode = NW_Cache.GetNode(child);

                node.Nodes.Add(ChildNode.ConvertToTreeNode());
            }

            return node;
        }
    }
}
