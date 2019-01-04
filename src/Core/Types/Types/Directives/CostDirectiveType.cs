namespace HotChocolate.Types
{
    public sealed class CostDirectiveType
        : DirectiveType
    {
        internal CostDirectiveType()
        {
        }

        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("cost");

            descriptor.Description(
                "The cost directives is used to express the complexity " +
                "of a field.");

            descriptor.Location(DirectiveLocation.FieldDefinition);

            descriptor.Argument("complexity")
                .Description("Defines the complexity of the field.")
                .Type<NonNullType<IntType>>()
                .DefaultValue(1);

            descriptor.Argument("multipliers")
                .Description(
                    "Defines field arguments that act as " +
                     "complexity multipliers.")
                .Type<ListType<NonNullType<StringType>>>();
        }
    }
}
