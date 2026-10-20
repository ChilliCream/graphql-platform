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
            // Preserve the generic type argument: [ExtendObjectType<T>] -> [ObjectType<T>]
            var newGenericName = genericName.WithIdentifier(SyntaxFactory.Identifier("ObjectType"));
            newAttribute = attribute.WithName(newGenericName);
        }
        else if (attribute.Name is QualifiedNameSyntax { Right: GenericNameSyntax qualifiedGenericName } qualifiedName)
        {
            // Preserve qualifier and generic type argument: [Ns.ExtendObjectType<T>] -> [Ns.ObjectType<T>]
            var newGenericName = qualifiedGenericName.WithIdentifier(SyntaxFactory.Identifier("ObjectType"));
            newAttribute = attribute.WithName(SyntaxFactory.QualifiedName(qualifiedName.Left, newGenericName));
        }
        else if (attribute.ArgumentList?.Arguments.Count == 1
            && attribute.ArgumentList.Arguments[0].Expression is TypeOfExpressionSyntax typeofExpression)
        {
            // Convert typeof() argument to generic: [ExtendObjectType(typeof(T))] -> [ObjectType<T>]
            var newGenericName = SyntaxFactory.GenericName(
                SyntaxFactory.Identifier("ObjectType"),
                SyntaxFactory.TypeArgumentList(
                    SyntaxFactory.SingletonSeparatedList(typeofExpression.Type)));

            // Preserve qualifier if present: [Ns.ExtendObjectType(typeof(T))] -> [Ns.ObjectType<T>]
            NameSyntax newFullName = attribute.Name is QualifiedNameSyntax q
                ? SyntaxFactory.QualifiedName(q.Left, newGenericName)
                : newGenericName;

            newAttribute = attribute.WithName(newFullName).WithArgumentList(null);
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
