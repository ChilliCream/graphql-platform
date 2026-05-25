using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ShareableScopedOnMemberCodeFixProvider))]
public sealed class ShareableScopedOnMemberCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0103"];

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
        var attributeSyntax = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attributeSyntax is null)
        {
            return;
        }

        const string title = "Remove 'scoped' argument from [Shareable]";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => RemoveScopedArgumentAsync(
                    context.Document,
                    attributeSyntax,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> RemoveScopedArgumentAsync(
        Document document,
        AttributeSyntax attribute,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || attribute.ArgumentList is null)
        {
            return document;
        }

        // Find and remove the "scoped" argument
        var newArguments = new List<AttributeArgumentSyntax>();
        foreach (var argument in attribute.ArgumentList.Arguments)
        {
            // Skip the "scoped" argument
            if (argument.NameColon?.Name.Identifier.Text == "scoped"
                || argument.NameEquals?.Name.Identifier.Text == "scoped")
            {
                continue;
            }

            newArguments.Add(argument);
        }

        AttributeSyntax newAttribute;

        // If no arguments remain, remove the argument list entirely
        if (newArguments.Count == 0)
        {
            newAttribute = attribute.WithArgumentList(null);
        }
        else
        {
            // Create new argument list with remaining arguments
            var separatedList = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.SeparatedList(newArguments);
            var newArgumentList = attribute.ArgumentList.WithArguments(separatedList);
            newAttribute = attribute.WithArgumentList(newArgumentList);
        }

        var newRoot = root.ReplaceNode(attribute, newAttribute);
        return document.WithSyntaxRoot(newRoot);
    }
}
