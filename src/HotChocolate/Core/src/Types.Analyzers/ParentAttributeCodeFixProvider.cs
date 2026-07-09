using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParentAttributeCodeFixProvider))]
public sealed class ParentAttributeCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0097"];

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

        // Find the parameter type syntax
        var node = root.FindNode(diagnosticSpan);
        var parameterSyntax = node.AncestorsAndSelf().OfType<ParameterSyntax>().FirstOrDefault();
        if (parameterSyntax?.Type is null)
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
        var methodDeclaration = parameterSyntax.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration?.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        var correctType = GetObjectTypeGenericArgument(classDeclaration, semanticModel);
        if (correctType is null)
        {
            return;
        }

        var title = $"Change type to '{correctType.Name}'";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => FixParameterTypeAsync(
                    context.Document,
                    parameterSyntax,
                    correctType,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> FixParameterTypeAsync(
        Document document,
        ParameterSyntax parameter,
        ITypeSymbol correctType,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null || parameter.Type is null)
        {
            return document;
        }

        // Create the new type syntax
        var newTypeSyntax = SyntaxFactory.ParseTypeName(correctType.ToDisplayString())
            .WithTriviaFrom(parameter.Type);

        // Replace the parameter type
        var newParameter = parameter.WithType(newTypeSyntax);
        var newRoot = root.ReplaceNode(parameter, newParameter);

        return document.WithSyntaxRoot(newRoot);
    }

    private static ITypeSymbol? GetObjectTypeGenericArgument(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel)
    {
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                // Check if this is ObjectTypeAttribute
                if (attributeType is not INamedTypeSymbol namedAttributeType
                    || namedAttributeType.Name != "ObjectTypeAttribute"
                    || namedAttributeType.ContainingNamespace?.ToDisplayString() != "HotChocolate.Types")
                {
                    continue;
                }

                // Check if it's a generic attribute and extract the type argument
                if (namedAttributeType.IsGenericType && namedAttributeType.TypeArguments.Length == 1)
                {
                    return namedAttributeType.TypeArguments[0];
                }
            }
        }

        return null;
    }
}
