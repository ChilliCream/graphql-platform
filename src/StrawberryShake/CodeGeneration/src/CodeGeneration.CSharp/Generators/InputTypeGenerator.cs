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
            return new(
                descriptor.Name,
                null,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                settings.InputRecords
                    ? GenerateRecord(descriptor, settings)
                    : GenerateClass(descriptor, settings));
        }

        private BaseTypeDeclarationSyntax GenerateRecord(
            InputObjectTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
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
                FieldDeclaration(prop.Type.ToTypeSyntax(), prop.Name)
            }

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

            return recordDeclarationSyntax;
        }

        private BaseTypeDeclarationSyntax GenerateClass(
            InputObjectTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
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

            return classDeclaration;
        }
    }

    public class InputTypeStateInterfaceGenerator : CSharpSyntaxGenerator<InputObjectTypeDescriptor>
    {
        protected override CSharpSyntaxGeneratorResult Generate(
            InputObjectTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
            InterfaceDeclarationSyntax interfaceDeclaration =
                InterfaceDeclaration(descriptor.Name.Value)
                    .AddModifiers(Token(SyntaxKind.InternalKeyword))
                    .AddGeneratedAttribute();

            foreach (var prop in descriptor.Properties)
            {
                PropertyDeclarationSyntax property =
                    PropertyDeclaration(ParseTypeName(TypeNames.Boolean), prop.Name + "HasValue")
                        .WithGetterAndSetter();

                if (prop.Type.IsNonNullableType() && !prop.Type.GetRuntimeType().IsValueType)
                {
                    property = property.WithSuppressNullableWarningExpression();
                }

                interfaceDeclaration = interfaceDeclaration.AddMembers(property);
            }

            return new(
                descriptor.Name,
                null,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                interfaceDeclaration);
        }
    }
}
