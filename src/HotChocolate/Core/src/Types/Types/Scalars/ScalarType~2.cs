using System;
using HotChocolate.Language;
using HotChocolate.Properties;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract class ScalarType<TRuntimeType, TLiteral>
        : ScalarType<TRuntimeType>
        where TLiteral : IValueNode
    {
        protected ScalarType(NameString name, BindingBehavior bind = BindingBehavior.Explicit)
            : base(name, bind)
        {
        }

        public sealed override bool IsInstanceOfType(IValueNode valueSyntax)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            return (valueSyntax is TLiteral casted && IsInstanceOfType(casted))
                || valueSyntax is NullValueNode;
        }

        protected virtual bool IsInstanceOfType(TLiteral valueSyntax)
        {
            return true;
        }

        public sealed override bool IsInstanceOfType(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return true;
            }

            if (runtimeValue is TRuntimeType t)
            {
                return IsInstanceOfType(t);
            }

            return false;
        }

        protected virtual bool IsInstanceOfType(TRuntimeType runtimeValue)
        {
            return true;
        }

        public sealed override object? ParseLiteral(
            IValueNode valueSyntax, bool withDefaults = true)
        {
            if (valueSyntax is null)
            {
                throw new ArgumentNullException(nameof(valueSyntax));
            }

            if (valueSyntax is TLiteral casted && IsInstanceOfType(casted))
            {
                return ParseLiteral(casted);
            }

            if (valueSyntax is NullValueNode)
            {
                return null;
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(Name, valueSyntax.GetType()),
                this);
        }

        protected abstract TRuntimeType ParseLiteral(TLiteral valueSyntax);

        public sealed override IValueNode ParseValue(object? runtimeValue)
        {
            if (runtimeValue is null)
            {
                return NullValueNode.Default;
            }

            if (runtimeValue is TRuntimeType t && IsInstanceOfType(t))
            {
                return ParseValue(t);
            }

            throw new SerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(Name, runtimeValue.GetType()),
                this);
        }

        protected abstract TLiteral ParseValue(TRuntimeType runtimeValue);
    }
}
