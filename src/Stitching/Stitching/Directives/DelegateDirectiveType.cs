using HotChocolate.Execution;
using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DelegateDirectiveType
        : DirectiveType<DelegateDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DelegateDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.Delegate);

            descriptor.Location(Types.DirectiveLocation.FieldDefinition);

            descriptor.Argument(t => t.Path)
                .Name(DirectiveFieldNames.Delegate_Path)
                .Type<StringType>();
        }
    }
}
