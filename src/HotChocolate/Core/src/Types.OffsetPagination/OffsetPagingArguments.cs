namespace HotChocolate.Types.Pagination
{
    public readonly struct OffsetPagingArguments
    {
        public OffsetPagingArguments(int? skip, int take)
        {
            Skip = skip;
            Take = take;
        }

        public int? Skip { get; }

        public int Take { get; }
    }
}
