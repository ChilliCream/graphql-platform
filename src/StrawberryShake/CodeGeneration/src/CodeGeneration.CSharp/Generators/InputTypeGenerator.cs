using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class InputTypeGenerator : CSharpSyntaxGenerator<InputObjectTypeDescriptor>
    {
        protected override CSharpSyntaxGeneratorResult Generate(
            InputObjectTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
            if (settings.InputRecords)
            {
                RecordDeclarationSyntax recordDeclarationSyntax =
                    RecordDeclaration(Token(SyntaxKind.RecordKeyword), descriptor.Name.Value)
                        .AddModifiers(
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.PartialKeyword))
                        .AddGeneratedAttribute()
                        .AddSummary(descriptor.Documentation)
                        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken));

                foreach (var prop in descriptor.Properties)
                {
                    recordDeclarationSyntax = recordDeclarationSyntax.AddMembers(
                        PropertyDeclaration(prop.Type.ToTypeSyntax(), prop.Name)
                            .AddModifiers(Token(SyntaxKind.PublicKeyword))
                            .AddSummary(prop.Description)
                            .WithGetterAndInit()
                            .WithSuppressNullableWarningExpression());
                }

                recordDeclarationSyntax = recordDeclarationSyntax.WithCloseBraceToken(
                    Token(SyntaxKind.CloseBraceToken));

                return new(
                    descriptor.Name,
                    null,
                    descriptor.RuntimeType.NamespaceWithoutGlobal,
                    recordDeclarationSyntax);
            }

            ClassDeclarationSyntax classDeclaration =
                ClassDeclaration(descriptor.Name.Value)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddSummary(descriptor.Documentation);

            foreach (var prop in descriptor.Properties)
            {
                classDeclaration = classDeclaration.AddMembers(
                    PropertyDeclaration(prop.Type.ToTypeSyntax(), prop.Name)
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .AddSummary(prop.Description)
                        .WithGetterAndSetter()
                        .WithSuppressNullableWarningExpression());
            }

            return new(
                descriptor.Name,
                null,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                classDeclaration);
        }
    }
}
