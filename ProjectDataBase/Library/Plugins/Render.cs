using Autodesk.Navisworks.Api;
using ProjectDataBase.Library.Interfaces;
using System.Collections.Generic;
using NW = Autodesk.Navisworks.Api;
using NW_Plugins = Autodesk.Navisworks.Api.Plugins;

namespace ProjectDataBase.Library.Plugins
{
    public class RenderObjects : NW_Plugins.RenderPlugin
    {
        private static List<IRenderable> ToRender = new List<IRenderable>();

        public override void Render(View view, Graphics graphics)
        {
            if (ToRender.Count == 0)
                return;

            foreach (var renderable in ToRender)
            {
                if (renderable.IsVisible)
                    renderable.Render(graphics);
            }
        }

        public static void ClearRenderList()
        {
            ToRender.Clear();
        }

        public static void RemoveFromRender(IEnumerable<IRenderable> renderables)
        {
            foreach (var renderable in renderables)
            {
                if (ToRender.Contains(renderable))
                    ToRender.Remove(renderable);
            }
        }

        public static void RemoveFromRender(IRenderable renderable)
        {
            if (ToRender.Contains(renderable))
                ToRender.Remove(renderable);
        }

        public static void AddToRender(IEnumerable<IRenderable> renderables)
        {
            foreach (var renderable in renderables)
            {
                if (!ToRender.Contains(renderable))
                    ToRender.Add(renderable);
            }
        }

        public static void AddToRender(IRenderable renderable)
        {
            if (!ToRender.Contains(renderable))
                ToRender.Add(renderable);
        }
    }
}
