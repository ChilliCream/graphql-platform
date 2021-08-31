using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class SortableDirectiveType : DirectiveType<SortableDirective>
    {
        protected override void Configure(IDirectiveTypeDescriptor<SortableDirective> descriptor)
        {
            descriptor
                .Name("sortable")
                .Location(DirectiveLocation.FieldDefinition |
                    DirectiveLocation.Schema |
                    DirectiveLocation.Scalar);

            descriptor
                .Argument(t => t.Direction)
                .Type<SortDirectionType>();
        }
    }
}
