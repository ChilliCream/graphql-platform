using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using HotChocolate.Types.Analyzers.Inspectors;

namespace HotChocolate.Types.Analyzers;

[Shared]
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataLoaderReturnTypeCodeFixProvider))]
public sealed class DataLoaderReturnTypeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Adjust signature to match <T>";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [ErrorCodes.Analyzers.DataLoaderReturnTypeInvalid];

    public override FixAllProvider GetFixAllProvider()
        => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken)
            .ConfigureAwait(false);

        if (root is null
            || semanticModel is null
            || root.FindNode(context.Diagnostics[0].Location.SourceSpan)
                .AncestorsAndSelf()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault() is not { } methodDeclaration
            || !TryGetContract(semanticModel, methodDeclaration, context.CancellationToken, out var contract))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                c => ChangeReturnTypeAsync(context.Document, methodDeclaration, contract, c),
                Title),
            context.Diagnostics);
    }

    private static async Task<Document> ChangeReturnTypeAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        DataLoaderContract contract,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        if (root is null
            || semanticModel?.GetDeclaredSymbol(methodDeclaration, cancellationToken) is not IMethodSymbol methodSymbol)
        {
            return document;
        }

        var resultType = contract.Kind is DataLoaderKind.Batch
            ? $"global::System.Collections.Generic.IReadOnlyDictionary<{GetTypeName(contract.KeyType)}, {GetTypeName(contract.ValueType)}>"
            : GetTypeName(contract.ValueType);
        var wrapper = GetAsyncWrapper(methodSymbol.ReturnType, semanticModel.Compilation);
        var newReturnType = SyntaxFactory
            .ParseTypeName($"{wrapper}<{resultType}>")
            .WithTriviaFrom(methodDeclaration.ReturnType);
        var newRoot = root.ReplaceNode(methodDeclaration.ReturnType, newReturnType);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool TryGetContract(
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken,
        out DataLoaderContract contract)
    {
        foreach (var attribute in GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
                     semanticModel,
                     methodDeclaration,
                     semanticModel.Compilation,
                     cancellationToken))
        {
            if (attribute.IsGeneric
                && attribute.Type.TypeArguments[0] is INamedTypeSymbol dataLoaderType
                && GenericDataLoaderAnalyzerHelper.TryResolveContract(
                    dataLoaderType,
                    semanticModel.Compilation,
                    out contract))
            {
                return true;
            }
        }

        contract = default;
        return false;
    }

    private static string GetAsyncWrapper(ITypeSymbol returnType, Compilation compilation)
    {
        if (returnType is INamedTypeSymbol { Arity: 1 } namedType
            && SymbolEqualityComparer.Default.Equals(
                namedType.ConstructedFrom,
                compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")))
        {
            return "global::System.Threading.Tasks.ValueTask";
        }

        return "global::System.Threading.Tasks.Task";
    }

    private static string GetTypeName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
