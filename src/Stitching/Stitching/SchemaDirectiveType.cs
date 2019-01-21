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
            descriptor.Location(Types.DirectiveLocation.FieldDefinition)
                .Location(Types.DirectiveLocation.Field);
        }
    }
}
