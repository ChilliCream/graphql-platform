namespace HotChocolate.Types
{
    public sealed class PaginationAmountType
        : IntTypeBase
    {
        public PaginationAmountType()
            : this(int.MaxValue)
        {
        }

        public PaginationAmountType(int max)
            : base("PaginationAmount", 0, max)
        {
        }
    }
}
