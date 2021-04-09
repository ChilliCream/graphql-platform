using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

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
                        constructor = constructor.AddStateParameter(property);
                    }

                    recordDeclarationSyntax = recordDeclarationSyntax.AddMembers(constructor);

                    foreach (PropertyDescriptor property in descriptor.Properties.Select(t =>
                        t.Value))
                    {
                        recordDeclarationSyntax = recordDeclarationSyntax.AddStateProperty(property);
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
                    constructor = constructor.AddStateParameter(property);
                }

                classDeclaration = classDeclaration.AddMembers(constructor);

                foreach (PropertyDescriptor property in descriptor.Properties.Select(t => t.Value))
                {
                    classDeclaration = classDeclaration.AddStateProperty(property);
                }
            }

            return new(
                descriptor.RuntimeType.Name,
                State,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                classDeclaration);
        }
    }
}
