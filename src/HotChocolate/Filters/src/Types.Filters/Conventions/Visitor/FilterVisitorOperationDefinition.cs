namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorOperationDefinition<T>
    {
        public object OperationKind { get; set; } = default!;

        public FilterOperationHandler<T>? Handler { get; set; }
    }
}
