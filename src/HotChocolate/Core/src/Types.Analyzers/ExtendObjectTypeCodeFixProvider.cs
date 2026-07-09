using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtendObjectTypeCodeFixProvider))]
public sealed class ExtendObjectTypeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0096"];

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

        // Find the attribute syntax
        var node = root.FindNode(diagnosticSpan);
        var attribute = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attribute is null)
        {
            return;
        }

        const string title = "Upgrade to ObjectType";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => ReplaceExtendObjectTypeAsync(
                    context.Document,
                    attribute,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> ReplaceExtendObjectTypeAsync(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Replace "ExtendObjectType" with "ObjectType"
        var newName = SyntaxFactory.IdentifierName("ObjectType");
        AttributeSyntax newAttribute;

        if (attribute.Name is GenericNameSyntax genericName)
        {
            // Preserve the generic type argument
            var newGenericName = genericName.WithIdentifier(
                SyntaxFactory.Identifier("ObjectType"));
            newAttribute = attribute.WithName(newGenericName);
        }
        else
        {
            // Shouldn't happen for ExtendObjectType, but handle it anyway
            newAttribute = attribute.WithName(newName);
        }

        var newRoot = root.ReplaceNode(attribute, newAttribute);
        return document.WithSyntaxRoot(newRoot);
    }
}
