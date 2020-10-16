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
            descriptor
                .Name(DirectiveNames.Source)
                .Description(StitchingResources.SourceDirectiveType_Description)
                .Repeatable();

            descriptor
                .Location(DirectiveLocation.Enum)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.Union)
                .Location(DirectiveLocation.InputObject)
                .Location(DirectiveLocation.FieldDefinition)
                .Location(DirectiveLocation.InputFieldDefinition)
                .Location(DirectiveLocation.ArgumentDefinition)
                .Location(DirectiveLocation.EnumValue);

            descriptor
                .Argument(t => t.Name)
                .Name(DirectiveFieldNames.Source_Name)
                .Type<NonNullType<NameType>>()
                .Description(StitchingResources
                    .SourceDirectiveType_Name_Description);

            descriptor
                .Argument(t => t.Schema)
                .Name(DirectiveFieldNames.Source_Schema)
                .Type<NonNullType<NameType>>()
                .Description(StitchingResources
                    .SourceDirectiveType_Schema_Description);
        }
    }
}
