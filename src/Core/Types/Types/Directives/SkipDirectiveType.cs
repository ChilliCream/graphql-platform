namespace HotChocolate.Types
{
    public sealed class SkipDirectiveType
        : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name(WellKnownDirectives.Skip);

            descriptor.Description(
                "Directs the executor to skip this field or " +
                "fragment when the `if` argument is true.");

            descriptor
                .Location(DirectiveLocation.Field
                    | DirectiveLocation.FragmentSpread
                    | DirectiveLocation.InlineFragment);

            descriptor.Argument(WellKnownDirectives.IfArgument)
                .Description("Skipped when true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
