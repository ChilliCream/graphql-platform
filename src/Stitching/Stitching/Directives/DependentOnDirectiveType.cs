using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class ComputedDirectiveType
        : DirectiveType<ComputedDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<ComputedDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Computed)
                .Location(Types.DirectiveLocation.FieldDefinition);
            descriptor.Argument(t => t.DependantOn)
                .Type<ListType<NonNullType<StringType>>>()
                .Description(
                    "Specifies the fields on which a computed " +
                    "field is dependent on.");
        }
    }
}
