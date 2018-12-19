using System;
using HotChocolate.Language;

namespace HotChocolate.Types
{
    /// <summary>
    /// The Boolean scalar type represents true or false.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Boolean
    /// </summary>
    [SpecScalar]
    public sealed class BooleanType
        : ScalarType
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanType"/> class.
        /// </summary>
        public BooleanType()
            : base("Boolean")
        {
        }

        public override string Description =>
            TypeResources.BooleanType_Description();

        public override Type ClrType => typeof(bool);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            return literal is BooleanValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is BooleanValueNode boolLiteral)
            {
                return boolLiteral.Value;
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

            if (value is bool b)
            {
                return new BooleanValueNode(b);
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

            if (value is bool)
            {
                return value;
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

            if (serialized is bool)
            {
                value = serialized;
                return true;
            }

            value = null;
            return false;
        }
    }
}
