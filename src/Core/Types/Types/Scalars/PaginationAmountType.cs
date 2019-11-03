using HotChocolate.Language;
using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class PaginationAmountType
        : IntegerTypeBase<int>
    {
        public PaginationAmountType()
            : this(byte.MaxValue)
        {
        }

        public PaginationAmountType(int max)
            : base(ScalarNames.PaginationAmount, 0, max)
        {
        }

        protected override int ParseLiteral(IntValueNode literal)
        {
            return literal.ToInt32();
        }

        protected override IntValueNode ParseValue(int value)
        {
            return new IntValueNode(value);
        }
    }
}
