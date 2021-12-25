using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
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
