using System;

namespace ProjectDataBase.Library.Types
{
    /// <summary>
    /// Represents a cache profile for a Navisworks item, containing its unique identifier, child identifiers,
    /// bounding box, and file path.
    /// </summary>
    public struct CacheProfile
    {
        public Guid Guid { get; private set; }
        public Guid[] Children { get; private set; }
        public Box Box { get; private set; }
        public string Path { get; private set; }

        private CacheProfile(Guid guid, Guid[] children, Box box, string path)
        {
            Guid = guid;
            Children = children;
            Box = box;
            Path = path;
        }

        public static CacheProfile Set(Guid guid, Guid[] children, Box box, string path) => 
            new CacheProfile(guid, children, box, path);
    }
}
