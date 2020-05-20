namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterVisitorOperationDefinition<T>
    {
        public FilterOperationKind OperationKind { get; set; }

        public FilterOperationHandler<T>? Handler { get; set; }
    }
}
