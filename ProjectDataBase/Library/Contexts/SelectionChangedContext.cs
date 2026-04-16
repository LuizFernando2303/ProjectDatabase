using Autodesk.Navisworks.Api;
using System;
using System.Collections.Generic;

public class SelectionChangedContext : EventArgs
{
    public IReadOnlyCollection<ModelItem> SelectedItems { get; }
    public bool FirstOnly { get; }

    public SelectionChangedContext(IReadOnlyCollection<ModelItem> selectedItems, bool firstOnly)
    {
        SelectedItems = selectedItems;
        FirstOnly = firstOnly;
    }
}