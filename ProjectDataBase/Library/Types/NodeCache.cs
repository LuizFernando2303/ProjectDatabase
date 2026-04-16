using System;

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
