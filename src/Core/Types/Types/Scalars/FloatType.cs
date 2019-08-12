using System;
using System.Globalization;
using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// The Float scalar type represents signed double‐precision fractional
    /// values as specified by IEEE 754. Response formats that support an
    /// appropriate double‐precision number type should use that type to
    /// represent this scalar.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Float
    /// </summary>
    [SpecScalar]
    public sealed class FloatType
        : ScalarType
    {
        public FloatType()
            : base("Float")
        {
            Description = TypeResources.FloatType_Description;
        }

        public override Type ClrType => typeof(double);

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

            if (literal is FloatValueNode floatLiteral
                && TryParseDouble(floatLiteral.Value, out _))
            {
                return true;
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float

            if (literal is IntValueNode intLiteral
                && TryParseDouble(intLiteral.Value, out _))
            {
                return true;
            }

            return false;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is NullValueNode)
            {
                return null;
            }

            if (literal is FloatValueNode floatLiteral
                && TryParseDouble(floatLiteral.Value, out double d))
            {
                return d;
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float

            if (literal is IntValueNode intLiteral
                && TryParseDouble(intLiteral.Value, out d))
            {
                return d;
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

            if (value is double d)
            {
                return new FloatValueNode(SerializeDouble(d));
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

            if (value is double d)
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

            if (serialized is double)
            {
                value = serialized;
                return true;
            }

            if (TryConvertSerialized(serialized, ScalarValueKind.Float, out double c)
                || TryConvertSerialized(serialized, ScalarValueKind.Integer, out c))
            {
                value = c;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryParseDouble(string value, out double d) =>
            double.TryParse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out d);

        private static string SerializeDouble(double value) =>
            value.ToString("E", CultureInfo.InvariantCulture);
    }
}
