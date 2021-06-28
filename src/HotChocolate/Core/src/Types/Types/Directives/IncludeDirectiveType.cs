using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class IncludeDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(WellKnownDirectives.Include)
                .Description(TypeResources.IncludeDirectiveType_TypeDescription)
                .Location(DirectiveLocation.Field)
                .Location(DirectiveLocation.FragmentSpread)
                .Location(DirectiveLocation.InlineFragment);

            descriptor
                .Argument(WellKnownDirectives.IfArgument)
                .Description(TypeResources.IncludeDirectiveType_IfDescription)
                .Type<NonNullType<BooleanType>>();
        }
    }
}
