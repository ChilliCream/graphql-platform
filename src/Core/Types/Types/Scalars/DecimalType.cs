using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DecimalType
        : ScalarType
    {
        public DecimalType()
            : base("Decimal")
        {
            Description = TypeResources.DecimalType_Description;
        }

        public override Type ClrType => typeof(decimal);

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

            if (literal is FloatValueNode floatLiteral)
            {
                return true;
            }

            if (literal is IntValueNode intLiteral)
            {
                return true;
            }

            return false;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            try
            {
                if (literal == null)
                {
                    throw new ArgumentNullException(nameof(literal));
                }

                if (literal is NullValueNode)
                {
                    return null;
                }

                if (literal is FloatValueNode floatLiteral)
                {
                    return floatLiteral.ToDecimal();
                }

                if (literal is IntValueNode intLiteral)
                {
                    return intLiteral.ToDecimal();
                }
            }
            catch (Exception ex)
            {
                throw new ScalarSerializationException(
                    TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                        Name, literal.GetType()), ex);
            }

            throw new ScalarSerializationException(
                TypeResourceHelper.Scalar_Cannot_ParseLiteral(
                    Name, literal.GetType()));
        }

        public override IValueNode ParseValue(object value)
        {
            if (value is null)
            {
                return NullValueNode.Default;
            }

            if (value is decimal d)
            {
                return new FloatValueNode(d);
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

            if (value is decimal d)
            {
                return d;
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

            if (serialized is decimal)
            {
                value = serialized;
                return true;
            }

            if (TryConvertSerialized(serialized, ValueKind.Float, out decimal c)
                || TryConvertSerialized(serialized, ValueKind.Integer, out c))
            {
                value = c;
                return true;
            }

            value = null;
            return false;
        }
    }
}
