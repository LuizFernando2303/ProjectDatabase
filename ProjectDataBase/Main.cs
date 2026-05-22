using Autodesk.Navisworks.Api.Plugins;
using Autodesk.Navisworks.Api;
using ProjectDataBase.Config;
using ProjectDataBase.ProjectExplorer;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace ProjectDataBase
{
    [Plugin("ProjectDataBase.Startup", "LF", DisplayName = "Startup")]
    public class StartupPlugin : EventWatcherPlugin
    {
        public override void OnLoaded()
        {
            try
            {
                Autodesk.Navisworks.Api.Application.ActiveDocumentChanged += OnActiveDocumentChanged;

                if (Autodesk.Navisworks.Api.Application.ActiveDocument != null)
                {
                    SubscribeToDocumentEvents(Autodesk.Navisworks.Api.Application.ActiveDocument);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on plugin load: {ex}");
            }
        }

        public override void OnUnloading()
        {
            try
            {
                Autodesk.Navisworks.Api.Application.ActiveDocumentChanged -= OnActiveDocumentChanged;

                if (Autodesk.Navisworks.Api.Application.ActiveDocument != null)
                {
                    UnsubscribeFromDocumentEvents(Autodesk.Navisworks.Api.Application.ActiveDocument);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error on plugin unload: {ex}");
            }
        }

        private void OnActiveDocumentChanged(object sender, EventArgs e)
        {
            var document = Autodesk.Navisworks.Api.Application.ActiveDocument;
            if (document == null) return;

            UnsubscribeFromDocumentEvents(document);
            SubscribeToDocumentEvents(document);

            EvaluateAndExecute(document);
        }

        private void OnDocumentFileNameChanged(object sender, EventArgs e)
        {
            if (sender is Document document)
            {
                EvaluateAndExecute(document);
            }
        }

        private void EvaluateAndExecute(Document document)
        {
            if (document.IsClear || string.IsNullOrEmpty(document.Title))
                return;

            ExecuteOnProjectOpen(document);
        }

        private void ExecuteOnProjectOpen(Document document)
        {
            Debug.WriteLine($"Projeto aberto com sucesso: {document.Title}");

            NW_Cache.Initialize();
        }

        private void SubscribeToDocumentEvents(Document doc)
        {
            doc.FileNameChanged += OnDocumentFileNameChanged;
        }

        private void UnsubscribeFromDocumentEvents(Document doc)
        {
            doc.FileNameChanged -= OnDocumentFileNameChanged;
        }
    }

    [Plugin("ProjectDataBase.Explorer", "LF", DisplayName = "Explorer", ToolTip = "")]
    public class ExplorerPlugin : AddInPlugin
    {
        private SynchronizationContext Sc;

        public override int Execute(params string[] parameters)
        {
            Sc = SynchronizationContext.Current;

            NW_Cache.Initialize();
            LoadUI(Sc);

            return 0;
        }

        private void LoadUI(SynchronizationContext context)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    var form = new Form
                    {
                        Text = "Explorer",
                        Width = 1200,
                        Height = 800
                    };

                    var control = new ExplorerUi(context)
                    {
                        Dock = DockStyle.Fill
                    };

                    form.Controls.Add(control);

                    System.Windows.Forms.Application.Run(form);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = false;
            thread.Start();
        }
    }

    [Plugin("___", "___", DisplayName = "___", ToolTip = "___")]
    public class Renderer : Library.Plugins.RenderObjects { }
}
