using Microsoft.CodeAnalysis.CSharp;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class InputTypeStateInterfaceGenerator : CSharpSyntaxGenerator<InputObjectTypeDescriptor>
{
    protected override CSharpSyntaxGeneratorResult Generate(
        InputObjectTypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        var name = NamingConventions.CreateInputValueInfo(descriptor.Name);

        var interfaceDeclaration =
            SyntaxFactory.InterfaceDeclaration(name)
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.InternalKeyword))
                .AddGeneratedAttribute();

        foreach (var prop in descriptor.Properties)
        {
            interfaceDeclaration = interfaceDeclaration.AddMembers(
                SyntaxFactory.PropertyDeclaration(
                        SyntaxFactory.ParseTypeName(TypeNames.Boolean),
                        NamingConventions.CreateIsSetProperty(prop.Name))
                    .WithGetter());
        }

        return new(
            name,
            State,
            $"{descriptor.RuntimeType.NamespaceWithoutGlobal}.{State}",
            interfaceDeclaration);
    }
}
