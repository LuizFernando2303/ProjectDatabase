using ProjectDataBase.Config;
using System;
using NW = Autodesk.Navisworks.Api;

namespace ProjectDataBase.Library.Types
{
    public class Chunk : Box
    {
        public int SubX { get; private set; }
        public int SubY { get; private set; }
        public int SubZ { get; private set; }
        public Box Parent { get; private set; }

        public Chunk(
            NW.Point3D min,
            NW.Point3D max,
            NW.Color color,
            double alpha,
            int subX,
            int subY,
            int subZ,
            Box parent
        ) : base(min, max, color, alpha)
        {
            SubX = subX;
            SubY = subY;
            SubZ = subZ;
            Parent = parent;

            ComputeStaticData();
        }

        public Guid GetParent()
        {
            return NW_Cache.FindByBox(Parent);
        }

        public new void ComputeStaticData()
        {
            var min = Min;
            var max = Max;

            Center = new NW.Point3D(
                (min.X + max.X) * 0.5,
                (min.Y + max.Y) * 0.5,
                (min.Z + max.Z) * 0.5
            );

            XVector = new NW.Vector3D(max.X - min.X, 0, 0);
            YVector = new NW.Vector3D(0, max.Y - min.Y, 0);
            ZVector = new NW.Vector3D(0, 0, max.Z - min.Z);

            double dx = max.X - min.X;
            double dy = max.Y - min.Y;
            double dz = max.Z - min.Z;

            BoundingRadius = System.Math.Sqrt(dx * dx + dy * dy + dz * dz) * 0.5;

            MaxRenderDistanceSq = MaxRenderDistance * MaxRenderDistance;
            MaxRenderDistanceWithSizeSq =
                (MaxRenderDistance + BoundingRadius) * (MaxRenderDistance + BoundingRadius);
        }
    }
}