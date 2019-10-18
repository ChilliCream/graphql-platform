using System;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// Scalar types represent primitive leaf values in a GraphQL type system.
    /// GraphQL responses take the form of a hierarchical tree;
    /// the leaves on these trees are GraphQL scalars.
    /// </summary>
    public abstract class ScalarType<TClrType, TLiteral>
        : ScalarType<TClrType>
        where TLiteral : IValueNode
    {
        protected ScalarType(NameString name) : base(name)
        {
        }

        public sealed override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return (literal is TLiteral casted && IsInstanceOfType(casted))
                || literal is NullValueNode;
        }

        protected virtual bool IsInstanceOfType(TLiteral literal)
        {
            return true;
        }

        public sealed override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is TLiteral casted && IsInstanceOfType(casted))
            {
                return ParseLiteral(casted);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        protected abstract TClrType ParseLiteral(TLiteral literal);

        public sealed override bool IsInstanceOfType(object value)
        {
            if (value is null)
            {
                return true;
            }

            if (value is TClrType t)
            {
                return IsInstanceOfType(t);
            }

            return false;
        }

        protected virtual bool IsInstanceOfType(TClrType value)
        {
            return true;
        }

        public sealed override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is TClrType t && IsInstanceOfType(t))
            {
                return ParseValue(t);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        protected abstract TLiteral ParseValue(TClrType value);
    }
}
