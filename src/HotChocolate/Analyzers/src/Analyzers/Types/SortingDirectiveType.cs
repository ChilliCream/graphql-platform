using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class SortingDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name("sorting")
                .Location(DirectiveLocation.FieldDefinition | DirectiveLocation.Schema);
        }
    }
}
