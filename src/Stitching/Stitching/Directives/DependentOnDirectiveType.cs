using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DependentOnDirectiveType
        : DirectiveType<DependentOnDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DependentOnDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.DependentOn)
                .Location(Types.DirectiveLocation.FieldDefinition);
            descriptor.Argument(t => t.Fields)
                .Type<NonNullType<ListType<NonNullType<StringType>>>>()
                .Description(
                    "Specifies the fields on which a computed " +
                    "field is dependent on.");
        }
    }
}
