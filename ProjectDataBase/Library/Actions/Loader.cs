using Autodesk.Navisworks.Api;
using System.Linq;

namespace ProjectDataBase.Library.Actions
{
    public class Loader
    {
        public Loader() 
        {
            ShowBoxesOnSelection();
        }

        private void ShowBoxesOnSelection()
        {
            var action = new HighlightBox();
            Application.ActiveDocument.CurrentSelection.Changed += (s, e) =>
            {
                var selection = Application.ActiveDocument
                    .CurrentSelection
                    .SelectedItems
                    .ToList();
                var context = new SelectionChangedContext(selection, false);
                action.Handler(s, context);
            };
        }
    }
}
