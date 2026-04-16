using Autodesk.Navisworks.Api;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace ProjectDataBase.Library.Identity
{
    public static class IdentityFunctions
    {
        /// <summary>
        /// Creates a new GUID for the given model item. If the model item already has an InstanceGuid, it returns that. Otherwise, it generates a deterministic GUID based on the item's properties and hierarchy by building a unique fingerprint string and hashing it to create the GUID. This ensures that the same model item will always receive the same GUID, providing consistency across sessions and allowing for reliable identification of model items based on their characteristics.
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static Guid GetNewGuid(ModelItem item)
        {
            if (item.InstanceGuid != Guid.Empty)
                return item.InstanceGuid;

            string fingerprint = BuildFingerprint(item);

            Guid guid = CreateDeterministicGuid(fingerprint);

            return guid;
        }

        /// <summary>
        /// Generates a unique fingerprint string for the specified model item based on its hierarchy, properties, and
        /// class display name.
        /// </summary>
        /// <param name="item">The model item for which to build the fingerprint.</param>
        /// <returns>A string representing the fingerprint of the model item.</returns>
        private static string BuildFingerprint(ModelItem item)
        {
            var sb = new StringBuilder();

            var path = item.AncestorsAndSelf
                .Select(x => Normalize(x.DisplayName));

            sb.Append(string.Join("/", path));
            sb.Append("||");

            BoundingBox3D box = item.BoundingBox();

            string coords = $"({box.Min.X:F6},{box.Min.Y:F6},{box.Min.Z:F6})-({box.Max.X:F6},{box.Max.Y:F6},{box.Max.Z:F6})";
            sb.Append(coords);
            sb.Append("||");

            var props = item.PropertyCategories
                .SelectMany(c => c.Properties)
                .Where(p => p.Value != null && !string.IsNullOrEmpty(p.DisplayName))
                .Select(p => $"{Normalize(p.DisplayName)}={Normalize(p.Value.ToString())}")
                .OrderBy(x => x);

            sb.Append(string.Join("|", props));
            sb.Append("||");

            sb.Append(Normalize(item.ClassDisplayName));

            return sb.ToString();
        }

        /// <summary>
        /// Creates a deterministic GUID based on the input string by hashing it with SHA256 and taking the first 16 bytes of the hash to form the GUID. This ensures that the same input string will always produce the same GUID, making it suitable for generating consistent identifiers for model items based on their properties and hierarchy.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static Guid CreateDeterministicGuid(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                byte[] guidBytes = new byte[16];

                Array.Copy(hash, guidBytes, 16);

                return new Guid(guidBytes);
            }   
        }

        /// <summary>
        /// Normalizes a string by trimming whitespace and converting to lower case. If the input is null, it returns an empty string.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string Normalize(string value)
        {
            return value?.Trim().ToLowerInvariant() ?? "";
        }
    }
}
