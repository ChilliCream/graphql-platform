using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class RuntimeTypeDirectiveType : DirectiveType<RuntimeTypeDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<RuntimeTypeDirective> descriptor)
    {
        descriptor.Name("runtimeType");
        descriptor.Argument(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Argument(t => t.ValueType).Type<BooleanType>();
        descriptor.Location(DirectiveLocation.Scalar);
    }
}
