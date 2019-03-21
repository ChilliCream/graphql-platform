namespace HotChocolate.Types
{
    public sealed class CostDirectiveType
        : DirectiveType<CostDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<CostDirective> descriptor)
        {
            descriptor.Name("cost");

            descriptor.Description(
                "The cost directives is used to express the complexity " +
                "of a field.");

            descriptor.Location(DirectiveLocation.FieldDefinition);

            descriptor.Argument(t => t.Complexity)
                .Name("complexity")
                .Description("Defines the complexity of the field.")
                .Type<NonNullType<IntType>>()
                .DefaultValue(1);

            descriptor.Argument(t => t.Multipliers)
                .Name("multipliers")
                .Description(
                    "Defines field arguments that act as " +
                     "complexity multipliers.")
                .Type<ListType<NonNullType<MultiplierPathType>>>();
        }
    }
}
