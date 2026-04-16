using Autodesk.Navisworks.Api;

namespace ProjectDataBase.Library.Types
{
    public struct CompactBox
    {
        public float MinX, MinY, MinZ;
        public float MaxX, MaxY, MaxZ;

        public static CompactBox From(Box b)
        {
            return new CompactBox
            {
                MinX = (float)b.Min.X,
                MinY = (float)b.Min.Y,
                MinZ = (float)b.Min.Z,
                MaxX = (float)b.Max.X,
                MaxY = (float)b.Max.Y,
                MaxZ = (float)b.Max.Z
            };
        }

        public Box ToBox()
        {
            return Box.CreateFromBoundingBox(
                new BoundingBox3D(
                    new Point3D(MinX, MinY, MinZ),
                    new Point3D(MaxX, MaxY, MaxZ)
                ));
        }
    }
}
