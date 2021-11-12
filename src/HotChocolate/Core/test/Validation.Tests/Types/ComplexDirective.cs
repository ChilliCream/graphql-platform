using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class ComplexDirective
    : DirectiveType
{
    protected override void Configure(
        IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Repeatable();

        descriptor.Name("complex");

        descriptor.Location(HotChocolate.Types.DirectiveLocation.Field);

        descriptor.Argument("anyArg")
            .Type<AnyType>();

        descriptor.Argument("stringArg")
            .Type<StringType>();
    }
}
