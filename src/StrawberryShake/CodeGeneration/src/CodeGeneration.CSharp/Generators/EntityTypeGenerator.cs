using Microsoft.CodeAnalysis.CSharp;
using StrawberryShake.CodeGeneration.Descriptors;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

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
            var recordDeclarationSyntax =
                RecordDeclaration(Token(SyntaxKind.RecordKeyword), descriptor.RuntimeType.Name)
                    .AddModifiers(
                        Token(SyntaxKind.PublicKeyword),
                        Token(SyntaxKind.PartialKeyword))
                    .AddGeneratedAttribute()
                    .AddSummary(descriptor.Documentation)
                    .WithOpenBraceToken(Token(SyntaxKind.OpenBraceToken));

            if (descriptor.Properties.Count > 0)
            {
                var constructor =
                    ConstructorDeclaration(descriptor.RuntimeType.Name)
                        .AddModifiers(Token(SyntaxKind.PublicKeyword));

                foreach (var property in descriptor.Properties.Select(t => t.Value))
                {
                    constructor = constructor.AddStateParameter(property);
                }

                recordDeclarationSyntax = recordDeclarationSyntax.AddMembers(constructor);

                foreach (var property in descriptor.Properties.Select(t => t.Value))
                {
                    recordDeclarationSyntax =
                        recordDeclarationSyntax.AddStateProperty(property);
                }
            }

            recordDeclarationSyntax =
                recordDeclarationSyntax.WithCloseBraceToken(
                    Token(SyntaxKind.CloseBraceToken));

            return new(
                descriptor.RuntimeType.Name,
                State,
                descriptor.RuntimeType.NamespaceWithoutGlobal,
                recordDeclarationSyntax);
        }

        var modifier = settings.AccessModifier == AccessModifier.Public
            ? SyntaxKind.PublicKeyword
            : SyntaxKind.InternalKeyword;

        var classDeclaration =
            ClassDeclaration(descriptor.RuntimeType.Name)
                .AddModifiers(
                    Token(modifier),
                    Token(SyntaxKind.PartialKeyword))
                .AddGeneratedAttribute()
                .AddSummary(descriptor.Documentation);

        if (descriptor.Properties.Count > 0)
        {
            var constructor =
                ConstructorDeclaration(descriptor.RuntimeType.Name)
                    .AddModifiers(Token(SyntaxKind.PublicKeyword));

            foreach (var property in descriptor.Properties.Select(t => t.Value))
            {
                constructor = constructor.AddStateParameter(property);
            }

            classDeclaration = classDeclaration.AddMembers(constructor);

            foreach (var property in descriptor.Properties.Select(t => t.Value))
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
