using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching;

public sealed class ComputedDirectiveType : DirectiveType<ComputedDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<ComputedDirective> descriptor)
    {
        descriptor.Name(DirectiveNames.Computed);

        descriptor.Location(DirectiveLocation.FieldDefinition);

        descriptor.Argument(t => t.DependantOn)
            .Name(DirectiveFieldNames.Computed_DependantOn)
            .Type<ListType<NonNullType<StringType>>>()
            .Description(StitchingResources.ComputedDirectiveType_Description);
    }
}
