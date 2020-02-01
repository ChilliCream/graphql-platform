using HotChocolate.Types;

namespace StrawberryShake.Generators.Types
{
    public class NameDirectiveType
        : DirectiveType<NameDirective>
    {
        protected override void Configure(
            IDirectiveTypeDescriptor<NameDirective> descriptor)
        {
            descriptor.Name("name")
                .Location(DirectiveLocation.EnumValue)
                .Location(DirectiveLocation.Object)
                .Location(DirectiveLocation.Interface)
                .Location(DirectiveLocation.Union)
                .Location(DirectiveLocation.Enum)
                .Location(DirectiveLocation.InputObject)
                .Location(DirectiveLocation.Scalar)
                .Argument(t => t.Value).Type<NonNullType<StringType>>();
        }
    }
}
