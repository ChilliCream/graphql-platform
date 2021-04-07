using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class EntityTypeGenerator : CSharpSyntaxGenerator<EntityTypeDescriptor>
    {
        protected override bool CanHandle(
            EntityTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings) =>
            !settings.NoStore;

        protected override CSharpSyntaxGeneratorResult Generate(
            EntityTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings)
        {
            if (settings.EntityRecords)
            {
                RecordDeclarationSyntax recordDeclarationSyntax =
                    RecordDeclaration(Token(SyntaxKind.RecordKeyword), descriptor.RuntimeType.Name)
                        .AddModifiers(
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.PartialKeyword))
                        .AddGeneratedAttribute()
                        .AddSummary(descriptor.Documentation)
                        .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken));

                if (descriptor.Properties.Count > 0)
                {
                    ConstructorDeclarationSyntax constructor =
                        ConstructorDeclaration(descriptor.RuntimeType.Name)
                            .AddModifiers(Token(SyntaxKind.PublicKeyword));

                    foreach (PropertyDescriptor property in descriptor.Properties.Select(t =>
                        t.Value))
                    {
                        constructor = AddParameter(constructor, property);
                    }

                    recordDeclarationSyntax = recordDeclarationSyntax.AddMembers(constructor);

                    foreach (PropertyDescriptor property in descriptor.Properties.Select(t =>
                        t.Value))
                    {
                        recordDeclarationSyntax =
                            AddProperty(recordDeclarationSyntax, property, true);
                    }
                }

                recordDeclarationSyntax = recordDeclarationSyntax.WithCloseBraceToken(
                    Token(SyntaxKind.CloseBraceToken));

                return new(
                    descriptor.RuntimeType.Name,
                    State,
                    descriptor.RuntimeType.NamespaceWithoutGlobal,
                    recordDeclarationSyntax);
            }

            ClassDeclarationSyntax classDeclaration =
                ClassDeclaration(descriptor.RuntimeType.Name)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddSummary(descriptor.Documentation);

            if (descriptor.Properties.Count > 0)
            {
                ConstructorDeclarationSyntax constructor =
                    ConstructorDeclaration(descriptor.RuntimeType.Name)
                        .AddModifiers(Token(SyntaxKind.PublicKeyword));

                foreach (PropertyDescriptor property in descriptor.Properties.Select(t =>
                    t.Value))
                {
                    constructor = AddParameter(constructor, property);
                }

                classDeclaration = classDeclaration.AddMembers(constructor);

                foreach (PropertyDescriptor property in descriptor.Properties.Select(t =>
                    t.Value))
                {
                    classDeclaration = AddProperty(classDeclaration, property, false);
                }
            }

            return new(
                descriptor.RuntimeType.Name,
                State,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                classDeclaration);
        }

        private T AddProperty<T>(
            T type,
            PropertyDescriptor property,
            bool init)
            where T : TypeDeclarationSyntax
        {
            PropertyDeclarationSyntax propertyDeclaration =
                PropertyDeclaration(property.Type.ToStateTypeSyntax(), property.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword))
                    .AddSummary(property.Description);

            propertyDeclaration = init
                ? propertyDeclaration.WithGetterAndInit()
                : propertyDeclaration.WithGetter();

            return (T)type.AddMembers(propertyDeclaration);
        }

        private ConstructorDeclarationSyntax AddParameter(
            ConstructorDeclarationSyntax constructor,
            PropertyDescriptor property)
        {
            string paramName;
            string propertyName;

            if (property.Name == WellKnownNames.TypeName)
            {
                paramName = WellKnownNames.TypeName;
                propertyName = $"this.{WellKnownNames.TypeName}";
            }
            else
            {
                paramName = GetParameterName(property.Name);
                propertyName = property.Name;
            }

            return constructor
                .AddParameterListParameters(
                    Parameter(Identifier(paramName))
                        .WithType(property.Type.ToStateTypeSyntax()))
                .AddBodyStatements(
                    ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            IdentifierName(propertyName),
                            IdentifierName(paramName))));
        }
    }
}
