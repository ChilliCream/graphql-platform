using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class SerializationTypeDirectiveType : DirectiveType<SerializationTypeDirective>
{
    protected override void Configure(
        IDirectiveTypeDescriptor<SerializationTypeDirective> descriptor)
    {
        descriptor.Name("serializationType");
        descriptor.Argument(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Location(DirectiveLocation.Scalar);
    }
}
