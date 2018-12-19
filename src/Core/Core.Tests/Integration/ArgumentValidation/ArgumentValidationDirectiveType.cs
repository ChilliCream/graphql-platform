using HotChocolate.Types;

namespace HotChocolate.Integration.ArgumentValidation
{
    public class ArgumentValidationDirectiveType
        : DirectiveType<ArgumentValidationDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<ArgumentValidationDirective> descriptor)
        {
            descriptor.Name("validate");
            descriptor.Location(Types.DirectiveLocation.ArgumentDefinition);
            descriptor.BindArguments(BindingBehavior.Explicit);
        }
    }
}
