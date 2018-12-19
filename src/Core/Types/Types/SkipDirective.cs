namespace HotChocolate.Types
{
    public class SkipDirective
        : DirectiveType
    {
        internal SkipDirective()
        {
        }

        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("skip");
            descriptor.Description(
                "Directs the executor to skip this field or " +
                "fragment when the `if` argument is true.");

            descriptor
                .Location(DirectiveLocation.Field | DirectiveLocation.FragmentSpread)
                .Location(DirectiveLocation.InlineFragment);

            descriptor.Argument("if")
                .Description("Skipped when true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
