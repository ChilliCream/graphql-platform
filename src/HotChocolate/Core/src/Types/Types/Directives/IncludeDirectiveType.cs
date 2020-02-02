namespace HotChocolate.Types
{
    public sealed class IncludeDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(WellKnownDirectives.Include);

            descriptor.Description(
                "Directs the executor to include this field or fragment " +
                "only when the `if` argument is true.");

            descriptor.Location(DirectiveLocation.Field)
                .Location(DirectiveLocation.FragmentSpread)
                .Location(DirectiveLocation.InlineFragment);

            descriptor.Argument(WellKnownDirectives.IfArgument)
                .Description("Included when true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
