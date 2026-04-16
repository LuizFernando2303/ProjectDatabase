using NW = Autodesk.Navisworks.Api;

namespace ProjectDataBase.Library.Interfaces
{
    public interface IRenderable
    {
        NW.Color Color { get; }
        double Alpha { get; }
        bool IsVisible { get; }

        void Render(NW.Graphics graphics);
    }
}
