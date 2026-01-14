using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParentMethodCodeFixProvider))]
public sealed class ParentMethodCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0098"];

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

        // Find the type argument syntax
        var node = root.FindNode(diagnosticSpan);

        // Get the GenericNameSyntax (Parent<T>)
        var genericName = node.AncestorsAndSelf().OfType<GenericNameSyntax>().FirstOrDefault();
        if (genericName is null || genericName.Identifier.Text != "Parent")
        {
            return;
        }

        // Get the semantic model
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel is null)
        {
            return;
        }

        // Find the containing class and get the ObjectType<T> generic argument
        var classDeclaration = node.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
        if (classDeclaration is null)
        {
            return;
        }

        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null)
        {
            return;
        }

        var correctType = GetObjectTypeGenericArgument(classSymbol);
        if (correctType is null)
        {
            return;
        }

        var title = $"Change type to '{correctType.Name}'";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => FixTypeArgumentAsync(
                    context.Document,
                    genericName,
                    correctType,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> FixTypeArgumentAsync(
        Document document,
        GenericNameSyntax genericName,
        ITypeSymbol correctType,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null)
        {
            return document;
        }

        // Create the new type argument
        var newTypeArgument = SyntaxFactory.ParseTypeName(correctType.ToDisplayString());

        // Replace the type argument in the type argument list
        var oldTypeArgument = genericName.TypeArgumentList.Arguments[0];
        var newTypeArgumentList = genericName.TypeArgumentList.WithArguments(
            SyntaxFactory.SingletonSeparatedList(newTypeArgument.WithTriviaFrom(oldTypeArgument)));

        // Create new GenericNameSyntax with updated type arguments
        var newGenericName = genericName.WithTypeArgumentList(newTypeArgumentList);

        // Replace in the tree
        var newRoot = root.ReplaceNode(genericName, newGenericName);
        return document.WithSyntaxRoot(newRoot);
    }

    private static ITypeSymbol? GetObjectTypeGenericArgument(INamedTypeSymbol classSymbol)
    {
        // Check the base type and its hierarchy
        var currentType = classSymbol.BaseType;
        while (currentType is not null)
        {
            // Check if this is ObjectType<T> or similar
            if (IsObjectTypeBase(currentType))
            {
                // Extract the generic argument
                if (currentType.IsGenericType && currentType.TypeArguments.Length == 1)
                {
                    return currentType.TypeArguments[0];
                }
            }

            currentType = currentType.BaseType;
        }

        return null;
    }

    private static bool IsObjectTypeBase(INamedTypeSymbol type)
    {
        // Check if this is ObjectType, ObjectTypeExtension, or similar base types
        var typeName = type.Name;
        var namespaceName = type.ContainingNamespace?.ToDisplayString();

        if (namespaceName != "HotChocolate.Types")
        {
            return false;
        }

        return typeName is "ObjectType"
            or "ObjectTypeExtension"
            or "InterfaceType"
            or "InterfaceTypeExtension";
    }
}
