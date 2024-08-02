using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Analyzers.Types;

public class RenameDirectiveType : DirectiveType<RenameDirective>
{
    protected override void Configure(IDirectiveTypeDescriptor<RenameDirective> descriptor)
    {
        descriptor.Name("rename");
        descriptor.Argument(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Location(
            DirectiveLocation.InputFieldDefinition |
            DirectiveLocation.InputObject |
            DirectiveLocation.Enum |
            DirectiveLocation.EnumValue);
    }
}
