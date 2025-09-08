using HotChocolate.Properties;

namespace HotChocolate.Types;

/// <summary>
/// Directs the executor to skip this field or fragment when the `if` argument is true.
/// </summary>
public sealed class SkipDirectiveType : DirectiveType
{
    protected override void Configure(IDirectiveTypeDescriptor descriptor)
    {
        descriptor
            .Name(DirectiveNames.Skip.Name)
            .Description(TypeResources.SkipDirectiveType_TypeDescription)
            .Location(DirectiveLocation.Field)
            .Location(DirectiveLocation.FragmentSpread)
            .Location(DirectiveLocation.InlineFragment);

        descriptor
            .Argument(DirectiveNames.Skip.Arguments.If)
            .Description(TypeResources.SkipDirectiveType_IfDescription)
            .Type<NonNullType<BooleanType>>();
    }
}
