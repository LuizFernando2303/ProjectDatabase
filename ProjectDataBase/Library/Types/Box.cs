using Autodesk.Navisworks.Api;
using NW = Autodesk.Navisworks.Api;

namespace ProjectDataBase.Library.Types
{
    public class Box : NW.BoundingBox3D, Interfaces.IRenderable
    {
        public NW.Color Color { get; set; }
        public double Alpha { get; set; }
        public bool IsVisible { get; set; } = true;

        public double MaxRenderDistance { get; set; } = 50.0;

        public double MaxRenderDistanceSq;
        public double MaxRenderDistanceWithSizeSq;

        public double BoundingRadius;

        public new NW.Point3D Center;

        public NW.Vector3D XVector;
        public NW.Vector3D YVector;
        public NW.Vector3D ZVector;

        public Chunk[,,] SubChunks { get; private set; }

        public Box(NW.Point3D min, NW.Point3D max, NW.Color color, double alpha)
            : base(min, max)
        {
            Color = color;
            Alpha = alpha;

            ComputeStaticData();
        }

        public double IntersectionVolume(Box other)
        {
            double xOverlap = System.Math.Max(0, System.Math.Min(Max.X, other.Max.X) - System.Math.Max(Min.X, other.Min.X));
            double yOverlap = System.Math.Max(0, System.Math.Min(Max.Y, other.Max.Y) - System.Math.Max(Min.Y, other.Min.Y));
            double zOverlap = System.Math.Max(0, System.Math.Min(Max.Z, other.Max.Z) - System.Math.Max(Min.Z, other.Min.Z));

            return xOverlap * yOverlap * zOverlap;
        }

        public void CreateSubChunks(int subX = 2, int subY = 2, int subZ = 1)
        {
            SubChunks = new Chunk[subX, subY, subZ];

            for (int x = 0; x < subX; x++)
            {
                for (int y = 0; y < subY; y++)
                {
                    for (int z = 0; z < subZ; z++)
                    {
                        var chunkMin = new NW.Point3D(
                            Min.X + XVector.X * x / subX,
                            Min.Y + YVector.Y * y / subY,
                            Min.Z + ZVector.Z * z / subZ
                        );

                        var chunkMax = new NW.Point3D(
                            Min.X + XVector.X * (x + 1) / subX,
                            Min.Y + YVector.Y * (y + 1) / subY,
                            Min.Z + ZVector.Z * (z + 1) / subZ
                        );

                        SubChunks[x, y, z] = new Chunk(
                            chunkMin,
                            chunkMax,
                            Color,
                            Alpha,
                            subX,
                            subY,
                            subZ,
                            this
                        );
                    }
                }
            }
        }

        public static Box CreateFromBoundingBox(NW.BoundingBox3D box, NW.Color color = null, double alpha = 1)
        {
            if (color == null)
                color = NW.Color.Red;

            return new Box(box.Min, box.Max, color, alpha);
        }

        public static Box CreateFromModelItem(ModelItem item, NW.Color color = null, double alpha = 1)
        {
            if (color == null)
                color = NW.Color.Red;

            NW.BoundingBox3D box = item.BoundingBox();
            return CreateFromBoundingBox(box, color, alpha);
        }

        public void ComputeStaticData()
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

        private static NW.Point3D GetCameraPosition()
        {
            var doc = NW.Application.ActiveDocument;
            return doc.CurrentViewpoint.ToViewpoint().Position;
        }

        public void Render(NW.Graphics graphics)
        {
            if (!IsVisible)
                return;

            var cam = GetCameraPosition();

            double dx = Center.X - cam.X;
            double dy = Center.Y - cam.Y;
            double dz = Center.Z - cam.Z;

            double distSq = dx * dx + dy * dy + dz * dz;

            if (distSq > MaxRenderDistanceWithSizeSq)
                return;

            graphics.Color(Color, Alpha);

            graphics.Cuboid(
                Min,
                XVector, // X width vector
                YVector, // Y width vector
                ZVector, // Z width vector
                false
            );
        }
    }
}