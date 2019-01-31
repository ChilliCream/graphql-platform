using HotChocolate.Types;

namespace HotChocolate.Stitching
{
    public class DependentOnDirectiveType
        : DirectiveType<DelegateDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<DelegateDirective> descriptor)
        {
            descriptor.Name(DirectiveNames.DependentOn)
                .Location(Types.DirectiveLocation.FieldDefinition);
        }
    }
}
