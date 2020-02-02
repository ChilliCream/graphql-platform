using HotChocolate.Types;

namespace StrawberryShake.Generators.Types
{
    public class SerializationDirectiveType
        : DirectiveType<SerializationDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<SerializationDirective> descriptor)
        {
            descriptor.Name("serialization");
            descriptor.Argument(t => t.ClrType).Type<NonNullType<StringType>>();
            descriptor.Argument(t => t.SerializationType).Type<NonNullType<StringType>>();
            descriptor.Location(DirectiveLocation.Field);
        }
    }
}
