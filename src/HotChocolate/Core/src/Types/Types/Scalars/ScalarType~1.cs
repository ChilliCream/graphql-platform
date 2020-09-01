using System;

#nullable enable

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
        protected ScalarType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
        }

        public sealed override Type RuntimeType => typeof(TClrType);

        public override bool TrySerialize(object? value, out object? serialized)
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

        public override bool TryDeserialize(object? resultValue, out object? runtimeValue)
        {
            if (resultValue is null)
            {
                runtimeValue = null;
                return true;
            }

            if (resultValue is TClrType)
            {
                runtimeValue = resultValue;
                return true;
            }

            runtimeValue = null;
            return false;
        }
    }
}
