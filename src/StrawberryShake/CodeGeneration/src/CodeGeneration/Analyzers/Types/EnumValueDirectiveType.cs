using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class EnumValueDirectiveType : DirectiveType<EnumValueDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<EnumValueDirective> descriptor)
    {
        descriptor.Name("enumValue");
        descriptor.Argument(t => t.Value).Type<NonNullType<StringType>>();
        descriptor.Location(DirectiveLocation.EnumValue);
    }
}
