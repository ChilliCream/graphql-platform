using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IdAttributeOnRecordParameterCodeFixProvider))]
public sealed class IdAttributeOnRecordParameterCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0105"];

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

        // Find the attribute
        var node = root.FindNode(diagnosticSpan);
        var attribute = node.AncestorsAndSelf().OfType<AttributeSyntax>().FirstOrDefault();
        if (attribute is null)
        {
            return;
        }

        // Find the attribute list
        var attributeList = attribute.Parent as AttributeListSyntax;
        if (attributeList is null)
        {
            return;
        }

        const string title = "Add 'property:' target to ID attribute";
        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => AddPropertyTargetAsync(
                    context.Document,
                    attributeList,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> AddPropertyTargetAsync(
        Document document,
        AttributeListSyntax attributeList,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Create new attribute list with property: target
        // This works for both [ID] and [ID<T>] syntax
        var propertyTarget = SyntaxFactory.AttributeTargetSpecifier(
            SyntaxFactory.Token(SyntaxKind.PropertyKeyword));

        var newAttributeList = attributeList.WithTarget(propertyTarget)
            .WithLeadingTrivia(attributeList.GetLeadingTrivia())
            .WithTrailingTrivia(attributeList.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(attributeList, newAttributeList);
        return document.WithSyntaxRoot(newRoot);
    }
}
