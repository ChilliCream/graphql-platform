using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class SourceDirectiveType
        : DirectiveType<SourceDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<SourceDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Source)
                .Description("Annotates the original name of a type.");

            descriptor.Location(DirectiveLocation.Enum)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.Union)
                .Location(DirectiveLocation.InputObject)
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.EnumValue);

            descriptor.Repeatable();

            descriptor.Argument(t => t.Name)
                .Name(DirectiveFieldNames.Renamed_Name)
                .Type<NonNullType<StringType>>()
                .Description("The original name of the annotated type.");

            descriptor.Argument(t => t.Schema)
                .Name(DirectiveFieldNames.Renamed_Schema)
                .Type<NonNullType<StringType>>()
                .Description("The name of the schema to which this " +
                    "type belongs to.");
        }
    }
}
