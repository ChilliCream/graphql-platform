using HotChocolate.Types;

namespace HotChocolate.Analyzers.Types
{
    public class ProjectionDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name("projection")
                .Location(DirectiveLocation.FieldDefinition | DirectiveLocation.Schema);
        }
    }
}
