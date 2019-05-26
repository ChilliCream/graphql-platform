using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed partial class CachedQuery
    {
        private readonly struct Key
            : IEquatable<Key>
        {
            public Key(SelectionSetNode fieldSelection, ObjectType type)
            {
                FieldSelection = fieldSelection
                    ?? throw new ArgumentNullException(nameof(fieldSelection));
                Type = type
                    ?? throw new ArgumentNullException(nameof(type));
            }

            public SelectionSetNode FieldSelection { get; }

            public ObjectType Type { get; }

            public bool Equals(Key other)
            {
                if (other.FieldSelection == null && FieldSelection == null)
                {
                    return true;
                }

                if (object.ReferenceEquals(other.FieldSelection, FieldSelection)
                    && object.ReferenceEquals(other.Type, Type))
                {
                    return true;
                }

                return false;
            }

            public override bool Equals(object obj)
            {
                if (obj is null)
                {
                    return false;
                }

                if (obj is Key key)
                {
                    return Equals(key);
                }

                return false;
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = (FieldSelection?.GetHashCode() ?? 0) * 397;
                    hash = hash ^ ((Type?.GetHashCode() ?? 0) * 7);
                    return hash;
                }
            }
        }
    }
}
