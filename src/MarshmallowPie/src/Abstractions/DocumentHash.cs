using System;

namespace MarshmallowPie
{
    public sealed class DocumentHash
        : IEquatable<DocumentHash>
    {
        public DocumentHash(string hash, string hashName)
        {
            Hash = hash;
            HashName = hashName;
        }

        public string Hash { get; }

        public string HashName { get; }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as DocumentHash);
        }

        public bool Equals(DocumentHash? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Hash.Equals(other.Hash, StringComparison.Ordinal)
                && HashName.Equals(other.HashName, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Hash, HashName);
        }
    }

}
