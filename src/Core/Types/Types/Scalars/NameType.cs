using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The name scalar represents a valid GraphQL name as specified in the spec
    /// and can be used to refer to fields or types.
    /// </summary>
    public sealed class NameType
        : ScalarType
    {
        public NameType()
            : base("Name")
        {
            Description = TypeResources.NameType_Description;
        }

        public override Type ClrType => typeof(NameString);

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
                && NameUtils.IsValidGraphQLName(s.Value);
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is StringValueNode stringLiteral)
            {
                if (!NameUtils.IsValidGraphQLName(stringLiteral.Value))
                {
                    throw new ScalarSerializationException(
                        string.Format(CultureInfo.InvariantCulture,
                            AbstractionResources.Type_NameIsNotValid,
                            stringLiteral.Value ?? "null"));
                }
                return new NameString(stringLiteral.Value);
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value == null)
            {
                return new NullValueNode(null);
            }

            if (value is NameString n)
            {
                return new StringValueNode(null, n, false);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseValue(
                    Name, value.GetType()));
        }

        public override object Serialize(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is NameString n)
            {
                return (string)n;
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_Serialize(Name));
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
                value = new NameString(s);
                return true;
            }

            if (serialized is NameString n)
            {
                value = n;
                return true;
            }

            value = null;
            return false;
        }
    }
}
