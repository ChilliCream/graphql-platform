using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class DecimalType
        : FloatTypeBase<decimal>
    {
        public DecimalType()
            : this(decimal.MinValue, decimal.MaxValue)
        {
        }

        public DecimalType(decimal min, decimal max)
            : this(ScalarNames.Decimal, min, max)
        {
            Description = TypeResources.DecimalType_Description;
        }

        public DecimalType(NameString name)
            : this(name, decimal.MinValue, decimal.MaxValue)
        {
        }

        public DecimalType(NameString name, decimal min, decimal max)
            : base(name, min, max)
        {
        }

        public DecimalType(NameString name, string description, decimal min, decimal max)
            : base(name, min, max)
        {
            Description = description;
        }

        protected override decimal ParseLiteral(IFloatValueLiteral literal)
        {
            return literal.ToDecimal();
        }

        protected override FloatValueNode ParseValue(decimal value)
        {
            return new FloatValueNode(value);
        }
    }
}
