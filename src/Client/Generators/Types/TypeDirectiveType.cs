using HotChocolate.Types;

namespace StrawberryShake.Generators.Types
{
    public class TypeDirectiveType
        : DirectiveType<TypeDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<TypeDirective> descriptor)
        {
            descriptor.Name("type");
            descriptor.Argument(t => t.Name).Type<NonNullType<StringType>>();
            descriptor.Location(DirectiveLocation.Field);
        }
    }
}
