using System;
using System.Collections.Generic;
using System.Globalization;
using HotChocolate.Language;

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
        }

        public override string Description =>
            TypeResources.FloatType_Description();

        public override Type ClrType => typeof(double);

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float
            return literal is FloatValueNode
                || literal is IntValueNode
                || literal is NullValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            if (literal is FloatValueNode floatLiteral)
            {
                return double.Parse(
                    floatLiteral.Value,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture);
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float
            if (literal is IntValueNode node)
            {
                return double.Parse(node.Value,
                    NumberStyles.Float,
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

            if (value is double d)
            {
                return new FloatValueNode(SerializeDouble(d));
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

            if (value is double d)
            {
                return d;
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

            if (serialized is double d)
            {
                value = d;
                return true;
            }

            value = null;
            return false;
        }

        private static double ParseDouble(string value) =>
            double.Parse(
                value,
                NumberStyles.Float,
                CultureInfo.InvariantCulture);

        private static string SerializeDouble(double value) =>
            value.ToString("E", CultureInfo.InvariantCulture);
    }
}
