using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectDataBase.Library.Types
{
    public class NodeCache
    {
        public Guid Parent;
        public Guid[] Children;
        public CompactBox? Space;
        public string Name;
    }
}
