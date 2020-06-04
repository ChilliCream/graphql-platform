namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorOperationDefinition<T>
    {
        public int OperationKind { get; set; } = default!;

        public FilterOperationHandler<T>? Handler { get; set; }
    }
}
