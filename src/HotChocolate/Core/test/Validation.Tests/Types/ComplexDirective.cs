using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    public class ComplexDirective
        : DirectiveType
    {
        protected override void Configure(
            IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Repeatable();

            descriptor.Name("complex");
            
            descriptor.Location(Types.DirectiveLocation.Field);

            descriptor.Argument("anyArg")
                .Type<AnyType>();

            descriptor.Argument("stringArg")
                .Type<StringType>();
        }
    }
}
