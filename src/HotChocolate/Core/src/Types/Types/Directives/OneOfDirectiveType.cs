using static HotChocolate.Properties.TypeResources;

namespace HotChocolate.Types;

/// <summary>
/// The `@oneOf` directive is used within the type system definition language
/// to indicate an Input Object is a Oneof Input Object.
/// </summary>
public sealed class OneOfDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
        => descriptor
            .Name(WellKnownDirectives.OneOf)
            .Description(OneOfDirectiveType_Description)
            .Location(DirectiveLocation.InputObject);
}
