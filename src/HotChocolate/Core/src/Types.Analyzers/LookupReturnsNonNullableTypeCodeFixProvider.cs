using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LookupReturnsNonNullableTypeCodeFixProvider))]
public sealed class LookupReturnsNonNullableTypeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0113"];

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

        var node = root.FindNode(diagnosticSpan);

        // Determine the type syntax to make nullable.
        TypeSyntax? typeSyntax = null;

        var methodDeclaration = node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration is not null)
        {
            typeSyntax = methodDeclaration.ReturnType;
        }
        else
        {
            var propertyDeclaration = node.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyDeclaration is not null)
            {
                typeSyntax = propertyDeclaration.Type;
            }
        }

        if (typeSyntax is null)
        {
            return;
        }

        const string title = "Make return type nullable";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => MakeReturnTypeNullableAsync(
                    context.Document,
                    typeSyntax,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> MakeReturnTypeNullableAsync(
        Document document,
        TypeSyntax typeSyntax,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        TypeSyntax newTypeSyntax;

        // If the type is Task<T> or ValueTask<T>, wrap the inner type argument.
        if (typeSyntax is GenericNameSyntax genericName
            && genericName.TypeArgumentList.Arguments.Count == 1
            && genericName.Identifier.Text is nameof(Task) or nameof(ValueTask))
        {
            var innerType = genericName.TypeArgumentList.Arguments[0];

            // Guard against double-wrapping.
            if (innerType is NullableTypeSyntax)
            {
                return document;
            }

            var nullableInnerType = SyntaxFactory.NullableType(innerType);
            var newTypeArgumentList = genericName.TypeArgumentList.WithArguments(
                SyntaxFactory.SingletonSeparatedList<TypeSyntax>(nullableInnerType));
            newTypeSyntax = genericName.WithTypeArgumentList(newTypeArgumentList);
        }
        else
        {
            // Guard against double-wrapping.
            if (typeSyntax is NullableTypeSyntax)
            {
                return document;
            }

            newTypeSyntax = SyntaxFactory.NullableType(typeSyntax);
        }

        newTypeSyntax = newTypeSyntax.WithTriviaFrom(typeSyntax);
        var newRoot = root.ReplaceNode(typeSyntax, newTypeSyntax);
        return document.WithSyntaxRoot(newRoot);
    }
}
