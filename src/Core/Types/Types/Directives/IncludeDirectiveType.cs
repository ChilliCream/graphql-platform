namespace HotChocolate.Types
{
    public sealed class IncludeDirectiveType
        : DirectiveType
    {
        internal IncludeDirectiveType()
        {
        }

        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("include");

            descriptor.Description(
                "Directs the executor to include this field or fragment " +
                "only when the `if` argument is true.");

            descriptor.Location(DirectiveLocation.Field)
                .Location(DirectiveLocation.FragmentSpread)
                .Location(DirectiveLocation.InlineFragment);

            descriptor.Argument("if")
                .Description("Included when true.")
                .Type<NonNullType<BooleanType>>();
        }
    }
}
