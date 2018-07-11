namespace HotChocolate.Types
{
    public class SkipDirective
        : Directive
    {
        internal SkipDirective()
        {
        }

        protected override void Configure(IDirectiveDescriptor descriptor)
        {
            descriptor.Name("skip");
            descriptor.Description(
                "Directs the executor to skip this field or " +
                "fragment when the `if` argument is true.");

            descriptor.Location(DirectiveLocation.Field)
                .Location(DirectiveLocation.FragmentSpread)
                .Location(DirectiveLocation.InlineFragment);

            descriptor.Argument("if")
                .Description("Skipped when true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
