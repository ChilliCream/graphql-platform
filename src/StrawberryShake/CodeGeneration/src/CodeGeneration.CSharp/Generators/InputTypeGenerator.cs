using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
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
                    PropertyDeclarationSyntax property =
                        PropertyDeclaration(prop.Type.ToTypeSyntax(), prop.Name)
                            .AddModifiers(Token(SyntaxKind.PublicKeyword))
                            .AddSummary(prop.Description)
                            .WithGetterAndInit();

                    if (prop.Type.IsNonNullableType() && !prop.Type.GetRuntimeType().IsValueType)
                    {
                        property = property.WithSuppressNullableWarningExpression();
                    }

                    recordDeclarationSyntax = recordDeclarationSyntax.AddMembers(property);
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

                PropertyDeclarationSyntax property =
                    PropertyDeclaration(prop.Type.ToTypeSyntax(), prop.Name)
                        .AddModifiers(Token(SyntaxKind.PublicKeyword))
                        .AddSummary(prop.Description)
                        .WithGetterAndSetter();

                if (prop.Type.IsNonNullableType() && !prop.Type.GetRuntimeType().IsValueType)
                {
                    property = property.WithSuppressNullableWarningExpression();
                }

                classDeclaration = classDeclaration.AddMembers(property);
            }

            return new(
                descriptor.Name,
                null,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                classDeclaration);
        }
    }
}
