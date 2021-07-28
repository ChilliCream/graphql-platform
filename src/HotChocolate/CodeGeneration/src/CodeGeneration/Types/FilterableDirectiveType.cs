using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class FilterableDirectiveType : DirectiveType<FilterableDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<FilterableDirective> descriptor)
        {
            descriptor
                .Name("filterable")
                .Location(DirectiveLocation.FieldDefinition |
                    DirectiveLocation.Schema |
                    DirectiveLocation.Scalar);

            descriptor
                .Argument(t => t.Operations)
                .Type<ListType<NonNullType<FilterOperationType>>>();
        }
    }
}
