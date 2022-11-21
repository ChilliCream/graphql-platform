using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching;

public sealed class DelegateDirectiveType : DirectiveType<DelegateDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<DelegateDirective> descriptor)
    {
        descriptor
            .Name(DirectiveNames.Delegate)
            .Description(StitchingResources.DelegateDirectiveType_Description)
            .Location(DirectiveLocation.FieldDefinition);

        descriptor
            .Argument(t => t.Path)
            .Name(DirectiveFieldNames.Delegate_Path)
            .Type<StringType>()
            .Description(StitchingResources.DelegateDirectiveType_Path_FieldDescription);

        descriptor
            .Argument(t => t.Schema)
            .Name(DirectiveFieldNames.Delegate_Schema)
            .Type<NonNullType<StringType>>()
            .Description(StitchingResources.DelegateDirectiveType_Schema_FieldDescription);
    }
}
