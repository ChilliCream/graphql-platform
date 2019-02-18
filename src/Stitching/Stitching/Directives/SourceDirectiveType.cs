using HotChocolate.Stitching.Properties;
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
                .Description(Resources.SourceDirectiveType_Description);

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
                .Description(Resources.SourceDirectiveType_Name_Description);

            descriptor.Argument(t => t.Schema)
                .Name(DirectiveFieldNames.Renamed_Schema)
                .Type<NonNullType<StringType>>()
                .Description(Resources.SourceDirectiveType_Schema_Description);
        }
    }
}
