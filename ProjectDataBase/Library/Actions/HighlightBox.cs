using Autodesk.Navisworks.Api;
using ProjectDataBase;
using ProjectDataBase.Config;
using ProjectDataBase.Library.Types;
using System;
using System.Linq;
using NW = Autodesk.Navisworks.Api;

public class HighlightBox : IAction<SelectionChangedContext>
{
    public EventHandler<SelectionChangedContext> Handler => Execute;

    public void Execute(object sender, SelectionChangedContext context)
    {
        if (context.SelectedItems == null)
            return;

        if (context.FirstOnly)
        {
            CacheProfile profile;
            ModelItem selected = context.SelectedItems.FirstOrDefault();
                
            NW_Cache.TryGetProfile(selected, out profile);

            Renderer.ClearRenderList();
            Renderer.AddToRender(profile.Box);

            Chunk chunk = NW_Cache.RootBoxes.FindLargestIntersection(profile.Box);
            chunk.Color = NW.Color.Blue;
            Renderer.AddToRender(chunk);

            return;
        }

        Renderer.ClearRenderList();
        foreach (var item in context.SelectedItems)
        {
            CacheProfile profile;
            NW_Cache.TryGetProfile(item, out profile);

            Renderer.AddToRender(profile.Box);

            Chunk chunk = NW_Cache.RootBoxes.FindLargestIntersection(profile.Box);
            chunk.Color = NW.Color.Blue;
            Renderer.AddToRender(chunk);
        }
    }
}