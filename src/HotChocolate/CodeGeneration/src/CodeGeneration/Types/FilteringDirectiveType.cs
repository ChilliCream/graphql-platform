using HotChocolate.Types;

namespace HotChocolate.CodeGeneration.Types
{
    public class FilteringDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name("filtering")
                .Location(DirectiveLocation.FieldDefinition | DirectiveLocation.Schema);
        }
    }
}
