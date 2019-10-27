using System;

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract class ScalarType<TClrType>
        : ScalarType
    {
        protected ScalarType(NameString name) : base(name)
        {
        }

        public sealed override Type ClrType => typeof(TClrType);

        public override bool TrySerialize(object value, out object serialized)
        {
            if (value is null)
            {
                serialized = null;
                return true;
            }

            if (value is TClrType)
            {
                serialized = value;
                return true;
            }

            serialized = null;
            return false;
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is TClrType)
            {
                value = serialized;
                return true;
            }

            value = null;
            return false;
        }
    }
}
