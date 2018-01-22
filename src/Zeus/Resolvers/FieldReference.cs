using System;
using System.Collections.Immutable;

namespace Zeus.Resolvers
{
    internal class FieldReference
        : IEquatable<FieldReference>
    {
        private static ImmutableDictionary<string, FieldReference> _cache = ImmutableDictionary<string, FieldReference>.Empty;

        private FieldReference(string typeName, string fieldName)
        {
            TypeName = typeName;
            FieldName = fieldName;
        }

        public string TypeName { get; }
        public string FieldName { get; }

        public bool Equals(FieldReference other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            if (ReferenceEquals(other, this))
            {
                return true;
            }

            return other.TypeName.Equals(TypeName, StringComparison.Ordinal)
                && other.FieldName.Equals(FieldName, StringComparison.Ordinal);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
            {
                return false;
            }

            if (ReferenceEquals(obj, this))
            {
                return true;
            }

            return Equals(obj as FieldReference);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (TypeName.GetHashCode() * 16777619)
                    ^ (FieldName.GetHashCode() * 16777619);
            }
        }

        public static FieldReference Create(string typeName, string fieldName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            string s = $"{typeName}->{fieldName}";
            if (!_cache.TryGetValue(s, out FieldReference f))
            {
                lock (_cache)
                {
                    f = new FieldReference(typeName, fieldName);
                    _cache = _cache.SetItem(s, f);
                }
            }
            return f;
        }
    }

}
