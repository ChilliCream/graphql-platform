using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class SchemaDirectiveType
        : DirectiveType<SchemaDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<SchemaDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Schema);

            descriptor.Location(Types.DirectiveLocation.FieldDefinition);

            descriptor.Argument(t => t.Name)
                .Name(DirectiveFieldNames.Schema_Name)
                .Type<NonNullType<NameType>>();
        }
    }
}
