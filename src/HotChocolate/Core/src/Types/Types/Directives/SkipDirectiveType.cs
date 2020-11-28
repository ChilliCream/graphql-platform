using HotChocolate.Properties;

namespace HotChocolate.Types
{
    public sealed class SkipDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor
                .Name(WellKnownDirectives.Skip)
                .Description(TypeResources.SkipDirectiveType_TypeDescription)
                .Location(DirectiveLocation.Field)
                .Location(DirectiveLocation.FragmentSpread)
                .Location(DirectiveLocation.InlineFragment);

            descriptor
                .Argument(WellKnownDirectives.IfArgument)
                .Description(TypeResources.SkipDirectiveType_IfDescription)
                .Type<NonNullType<BooleanType>>();
        }
    }
}
