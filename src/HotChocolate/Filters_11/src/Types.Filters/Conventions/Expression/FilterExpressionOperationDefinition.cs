namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterExpressionOperationDefinition
    {
        public FilterOperationKind OperationKind { get; set; }

        public FilterOperationHandler? Handler { get; set; }
    }
}
