using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(QueryContextConnectionCodeFixProvider))]
public sealed class QueryContextConnectionCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["HC0101"];

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

        // Find the containing method declaration
        var methodDeclaration = parameterSyntax.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDeclaration is null)
        {
            return;
        }

        // Get the correct type from the return type's IConnection<TNode>
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol is null)
        {
            return;
        }

        var correctType = GetConnectionNodeType(methodSymbol.ReturnType);
        if (correctType is null)
        {
            return;
        }

        var title = $"Change to QueryContext<{correctType.Name}>";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: title,
                createChangedDocument: c => FixQueryContextTypeAsync(
                    context.Document,
                    parameterSyntax,
                    correctType,
                    c),
                equivalenceKey: title),
            diagnostic);
    }

    private static async Task<Document> FixQueryContextTypeAsync(
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

        // Create the new QueryContext<CorrectType> syntax
        var correctTypeNameSyntax = SyntaxFactory.ParseTypeName(correctType.ToDisplayString());
        var genericName = SyntaxFactory.GenericName(
            SyntaxFactory.Identifier("QueryContext"),
            SyntaxFactory.TypeArgumentList(
                SyntaxFactory.SingletonSeparatedList(correctTypeNameSyntax)));

        var newTypeSyntax = genericName.WithTriviaFrom(parameter.Type);

        // Replace the parameter type
        var newParameter = parameter.WithType(newTypeSyntax);
        var newRoot = root.ReplaceNode(parameter, newParameter);

        return document.WithSyntaxRoot(newRoot);
    }

    private static ITypeSymbol? GetConnectionNodeType(ITypeSymbol returnType)
    {
        // Unwrap Task<T> or ValueTask<T>
        if (returnType is INamedTypeSymbol namedReturnType)
        {
            if (namedReturnType.Name is "Task" or "ValueTask"
                && namedReturnType.TypeArguments.Length == 1)
            {
                returnType = namedReturnType.TypeArguments[0];
            }
        }

        // Check if return type implements IConnection<TNode>
        if (returnType is not INamedTypeSymbol connectionType)
        {
            return null;
        }

        // Check if the type itself is a generic Connection type (like Connection<T>)
        if (IsConnectionType(connectionType))
        {
            return connectionType.TypeArguments.Length == 1 ? connectionType.TypeArguments[0] : null;
        }

        // Check if the type itself is IConnection<T>
        if (IsConnectionInterface(connectionType))
        {
            return connectionType.TypeArguments.Length == 1 ? connectionType.TypeArguments[0] : null;
        }

        // Check implemented interfaces
        foreach (var iface in connectionType.AllInterfaces)
        {
            if (IsConnectionInterface(iface))
            {
                return iface.TypeArguments.Length == 1 ? iface.TypeArguments[0] : null;
            }
        }

        return null;
    }

    private static bool IsConnectionType(INamedTypeSymbol type)
    {
        // Check if it's a generic type with "Connection" in the name from the pagination namespace
        if (!type.IsGenericType || !type.Name.Contains("Connection"))
        {
            return false;
        }

        var namespaceName = type.ContainingNamespace?.ToDisplayString();
        return namespaceName == "HotChocolate.Types.Pagination";
    }

    private static bool IsConnectionInterface(INamedTypeSymbol type)
    {
        if (type.Name != "IConnection")
        {
            return false;
        }

        var namespaceName = type.ContainingNamespace?.ToDisplayString();
        return namespaceName == "HotChocolate.Types.Pagination";
    }
}
