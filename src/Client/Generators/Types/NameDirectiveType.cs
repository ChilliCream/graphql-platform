using HotChocolate.Types;

namespace StrawberryShake.Generators.Types
{
    public class NameDirectiveType
        : DirectiveType<NameDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<NameDirective> descriptor)
        {
            descriptor.Argument(t => t.Value).Type<NonNullType<StringType>>();
            descriptor.Location(DirectiveLocation.EnumValue);
        }
    }
}
