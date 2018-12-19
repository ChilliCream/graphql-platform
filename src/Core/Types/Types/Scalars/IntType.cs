using System;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The Int scalar type represents a signed 32‐bit numeric non‐fractional
    /// value. Response formats that support a 32‐bit integer or a number type
    /// should use that type to represent this scalar.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Int
    /// </summary>
    [SpecScalar]
    public sealed class IntType
        : ScalarType
    {
        public IntType()
            : base("Int")
        {
        }

        public override string Description =>
            TypeResources.IntType_Description();

        public override Type ClrType => typeof(int);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is IntValueNode intLiteral)
            {
                return int.Parse(
                    intLiteral.Value,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()),
                nameof(literal));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is int i)
            {
                return new IntValueNode(i);
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()),
                nameof(value));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is int i)
            {
                return i;
            }

            throw new ArgumentException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is int i)
            {
                value = i;
                return true;
            }

            value = null;
            return false;
        }
    }
}
