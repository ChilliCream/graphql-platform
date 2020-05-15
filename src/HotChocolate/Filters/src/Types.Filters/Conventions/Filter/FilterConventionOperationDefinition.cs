namespace HotChocolate.Types.Filters.Conventions
{
    public class FilterConventionOperationDefinition
    {
        public CreateFieldName? Name { get; set; }

        public string? Description { get; set; }

        public bool Ignore { get; set; }

        public FilterOperationKind OperationKind { get; set; }
    }
}
