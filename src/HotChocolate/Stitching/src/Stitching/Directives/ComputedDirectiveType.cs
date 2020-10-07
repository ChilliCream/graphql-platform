using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class ComputedDirectiveType
        : DirectiveType<ComputedDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<ComputedDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Computed);

            descriptor.Location(DirectiveLocation.FieldDefinition);

            descriptor.Argument(t => t.DependantOn)
                .Name(DirectiveFieldNames.Computed_DependantOn)
                .Type<ListType<NonNullType<NameType>>>()
                .Description(StitchingResources.ComputedDirectiveType_Description);
        }
    }
}
