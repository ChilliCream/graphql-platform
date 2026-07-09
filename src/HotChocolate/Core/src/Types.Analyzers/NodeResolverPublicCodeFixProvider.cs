using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NodeResolverPublicCodeFixProvider))]
public sealed class NodeResolverPublicCodeFixProvider : CodeFixProvider
{
    private const string Title = "Make method public";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0093"];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return;
        }

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // Find the method declaration identified by the diagnostic
        var methodDeclaration = root.FindToken(diagnosticSpan.Start)
            .Parent?
            .AncestorsAndSelf()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (methodDeclaration is null)
        {
            return;
        }

        // Register a code action that will invoke the fix
        context.RegisterCodeFix(
            CodeAction.Create(
                title: Title,
                createChangedDocument: c => MakeMethodPublicAsync(
                    context.Document,
                    methodDeclaration,
                    c),
                equivalenceKey: Title),
            diagnostic);
    }

    private static async Task<Document> MakeMethodPublicAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        // Remove any existing access modifiers (private, protected, internal)
        var modifiersToRemove = methodDeclaration.Modifiers
            .Where(m => m.IsKind(SyntaxKind.PrivateKeyword)
                || m.IsKind(SyntaxKind.ProtectedKeyword)
                || m.IsKind(SyntaxKind.InternalKeyword))
            .ToList();

        var newModifiers = methodDeclaration.Modifiers;

        foreach (var modifier in modifiersToRemove)
        {
            newModifiers = newModifiers.Remove(modifier);
        }

        // Add the public keyword
        var publicKeyword = SyntaxFactory.Token(SyntaxKind.PublicKeyword)
            .WithTrailingTrivia(SyntaxFactory.Space);

        // Insert public at the beginning
        newModifiers = newModifiers.Insert(0, publicKeyword);

        var newMethodDeclaration = methodDeclaration.WithModifiers(newModifiers);

        // Replace the old method declaration with the new one
        var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);

        return document.WithSyntaxRoot(newRoot);
    }
}
