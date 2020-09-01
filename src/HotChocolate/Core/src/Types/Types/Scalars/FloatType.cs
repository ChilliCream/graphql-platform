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
        : FloatTypeBase<double>
    {
        public FloatType()
            : this(double.MinValue, double.MaxValue)
        {
        }

        public FloatType(double min, double max)
            : this(ScalarNames.Float, min, max)
        {
            Description = TypeResources.FloatType_Description;
        }

        public FloatType(NameString name)
            : this(name, double.MinValue, double.MaxValue)
        {
        }

        public FloatType(NameString name, double min, double max)
            : base(name, min, max, BindingBehavior.Implicit)
        {
        }

        public FloatType(NameString name, string description, double min, double max)
            : base(name, min, max, BindingBehavior.Implicit)
        {
            Description = description;
        }

        protected override double ParseLiteral(IFloatValueLiteral valueSyntax)
        {
            return valueSyntax.ToDouble();
        }

        protected override FloatValueNode ParseValue(double runtimeValue)
        {
            return new FloatValueNode(runtimeValue);
        }
    }
}
