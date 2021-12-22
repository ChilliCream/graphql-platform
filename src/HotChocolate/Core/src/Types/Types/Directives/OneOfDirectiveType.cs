using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types;

public class OneOfDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name("oneOf")
            .Description(OneOfDirectiveType_Description)
            .Location(
                DirectiveLocation.InputObject |
                DirectiveLocation.FieldDefinition);
}
