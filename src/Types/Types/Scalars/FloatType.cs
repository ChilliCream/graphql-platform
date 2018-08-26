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
    public sealed class FloatType
        : NumberType<double, FloatValueNode>
    {
        public FloatType()
            : base("Float")
        {
        }

        public override bool IsInstanceOfType(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
            }

            // Input coercion rules specify that float values can be coerced
            // from IntValueNode and FloatValueNode:
            // http://facebook.github.io/graphql/June2018/#sec-Float
            return base.IsInstanceOfType(literal) || literal is IntValueNode;
        }

        public override object ParseLiteral(IValueNode literal)
        {
            if (literal == null)
            {
                throw new ArgumentNullException(nameof(literal));
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

            return base.ParseLiteral(literal);
        }

        protected override double OnParseLiteral(FloatValueNode node) =>
            double.Parse(node.Value, NumberStyles.Float, CultureInfo.InvariantCulture);

        protected override FloatValueNode OnParseValue(double value) =>
            new FloatValueNode(value.ToString("E", CultureInfo.InvariantCulture));

        protected override IEnumerable<Type> AdditionalTypes =>
             new[] { typeof(float) };
    }
}
