using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class InputTypeStateInterfaceGenerator : CSharpSyntaxGenerator<InputObjectTypeDescriptor>
    {
        protected override CSharpSyntaxGeneratorResult Generate(
            InputObjectTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
            InterfaceDeclarationSyntax interfaceDeclaration =
                SyntaxFactory.InterfaceDeclaration(descriptor.Name.Value)
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
                descriptor.Name,
                null,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                interfaceDeclaration);
        }
    }
}
