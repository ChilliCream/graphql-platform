using HotChocolate.Stitching.Properties;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateDirectiveType
        : DirectiveType<DelegateDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DelegateDirective> descriptor)
        {
            descriptor
                .Name(DirectiveNames.Delegate)
                .Location(Types.DirectiveLocation.FieldDefinition)
                .Repeatable();

            descriptor.Argument(t => t.Path)
                .Name(DirectiveFieldNames.Delegate_Path)
                .Type<StringType>();

            descriptor.Argument(t => t.Schema)
                .Name(DirectiveFieldNames.Delegate_Schema)
                .Type<NonNullType<NameType>>()
                .Description(StitchingResources
                    .DelegateDirectiveType_Description);
        }
    }
}
