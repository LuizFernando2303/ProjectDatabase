using Newtonsoft.Json;
using ProjectDataBase.Config;
using ProjectDataBase.Library.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace ProjectDataBase.ProjectExplorer
{
    public partial class ExplorerUi : UserControl
    {
        private readonly SynchronizationContext NavisworksContext;

        private ConfigRoot CurrentConfig;

        public ExplorerUi(SynchronizationContext context)
        {
            NavisworksContext = context;

            InitializeComponent();

            LoadConfigJson();
            LoadTree();
        }

        private void LoadTree()
        {
            ProjectTree.Nodes.Clear();

            NodeCache rootNode = NW_Cache.GetRootNode();
            TreeNode treeNode = rootNode.ConvertToTreeNode();

            ProjectTree.Nodes.Add(treeNode);
        }

        private void LoadConfigJson()
        {
            try
            {
                string currentFilePath =
                    System.Reflection.Assembly
                        .GetExecutingAssembly()
                        .Location;

                string configPath =
                    Path.Combine(
                        Path.GetDirectoryName(currentFilePath),
                        "config.json");

                if (!File.Exists(configPath))
                {
                    MessageBox.Show(
                        $"Config file not found:\n{configPath}",
                        "Config",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    CurrentConfig = new ConfigRoot();

                    return;
                }

                string json = File.ReadAllText(configPath);

                CurrentConfig =
                    JsonConvert.DeserializeObject<ConfigRoot>(json)
                    ?? new ConfigRoot();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Config Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                CurrentConfig = new ConfigRoot();
            }
        }

        private void OnNodeMouseClick(object sender, TreeViewEventArgs e)
        {
            // grab all the avalible node data 
            TreeNode treeNode = e.Node;
            NodeCache nodeCache = (NodeCache)treeNode.Tag;
            Guid nodeCacheGuid = NW_Cache.GetGuid(nodeCache);
            Library.Types.ElementProperty[] properties = NW_Cache.GetProperties(nodeCacheGuid);

            // create DataGrid rows for each property
            List<DataGridViewRow> rows = new List<DataGridViewRow>();

            foreach (Library.Types.ElementProperty property in properties)
            {
                rows.Add(new DataGridViewRow()
                {
                    Cells =
                    {
                        new DataGridViewTextBoxCell() { Value = property.Name },
                        new DataGridViewTextBoxCell() { Value = property.Category },
                        new DataGridViewTextBoxCell() { Value = property.Value }
                    }
                });
            }

            // display in dataGrid
            CurrentNodeData.Rows.Clear();
            CurrentNodeData.Rows.AddRange(rows.ToArray());

            // Create child properties tree
            TreeNode tree = Utils.CreateNodeChildrenProperties(treeNode);
            AvalibleChildData.Nodes.Clear();
            AvalibleChildData.Nodes.Add(tree);
        }

        private bool ExecuteOnThread(Action action)
        {
            try
            {
                NavisworksContext.Post(_ => action(), null);

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.ToString(),
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                return false;
            }
        }
    }
}