using HotChocolate.Language;

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
            : base(ScalarNames.PaginationAmount, 0, max, BindingBehavior.Explicit)
        {
        }

        protected override int ParseLiteral(IntValueNode valueSyntax)
        {
            return valueSyntax.ToInt32();
        }

        protected override IntValueNode ParseValue(int runtimeValue)
        {
            return new IntValueNode(runtimeValue);
        }
    }
}
