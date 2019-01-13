using System;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types
{
    /// <summary>
    /// The name scalar represents a valid GraphQL name as specified in the spec
    /// and can be used to refer to fields or types.
    /// </summary>
    public sealed class MultiplierPathType
        : ScalarType
    {
        public MultiplierPathType()
            : base("MultiplierPath")
        {
        }

        public override string Description =>
            TypeResources.MultiplierPathType_Description();

        public override Type ClrType => typeof(MultiplierPathString);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return true;
            }

            return literal is StringValueNode s
                && MultiplierPathString.IsValidName(s.Value);
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral)
            {
                if (!MultiplierPathString.IsValidName(stringLiteral.Value))
                {
                    throw new ScalarSerializationException(
                        AbstractionResources.Type_Name_IsNotValid(
                            stringLiteral.Value));
                }
                return new MultiplierPathString(stringLiteral.Value);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is MultiplierPathString n)
            {
                return new StringValueNode(null, n, false);
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is MultiplierPathString n)
            {
                return (string)n;
            }

            throw new ScalarSerializationException(
                TypeResources.Scalar_Cannot_Serialize(Name));
        }

        public override bool TryDeserialize(object serialized, out object value)
        {
            if (serialized is null)
            {
                value = null;
                return true;
            }

            if (serialized is string s)
            {
                value = new MultiplierPathString(s);
                return true;
            }

            if (serialized is MultiplierPathString n)
            {
                value = n;
                return true;
            }

            value = null;
            return false;
        }
    }
}
