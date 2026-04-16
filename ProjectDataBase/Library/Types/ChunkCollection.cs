using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDataBase.Library.Types
{
    public class ChunkCollection : List<Chunk>
    {
        public Chunk FindSmallestIntersection(Box box)
        {
            var valid = this.Where(c => c.Intersects(box));

            return valid
                .OrderBy(c => c.IntersectionVolume(box))
                .FirstOrDefault();
        }

        public Chunk FindLargestIntersection(Box box)
        {
            var valid = this.Where(c => c.Intersects(box));

            return valid
                .OrderByDescending(c => c.IntersectionVolume(box))
                .FirstOrDefault();
        }
    }
}
