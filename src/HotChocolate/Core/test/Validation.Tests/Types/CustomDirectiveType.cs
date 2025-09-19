using HotChocolate.Types;

namespace HotChocolate.Validation.Types;

public class CustomDirectiveType
    : DirectiveType
{
    private readonly string _name;

    public CustomDirectiveType(string name)
    {
        _name = name;
    }

    protected override void Configure(
        IDirectiveTypeDescriptor descriptor)
    {
        descriptor.Repeatable();

        descriptor.Name(_name);

        descriptor.Location(DirectiveLocation.Field);

        descriptor.Argument("arg")
            .Type<StringType>();
        descriptor.Argument("arg1")
            .Type<StringType>();
        descriptor.Argument("arg2")
            .Type<StringType>();
        descriptor.Argument("arg3")
            .Type<StringType>();
        descriptor.Argument("arg4")
            .Type<StringType>();
    }
}
