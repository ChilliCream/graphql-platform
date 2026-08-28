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
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DataLoaderKeyParameterCodeFixProvider))]
public sealed class DataLoaderKeyParameterCodeFixProvider : CodeFixProvider
{
    private const string Title = "Adjust signature to match <T>";

    public override ImmutableArray<string> FixableDiagnosticIds { get; } =
        [ErrorCodes.Analyzers.DataLoaderKeyParameterInvalid];

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
            || methodDeclaration.ParameterList.Parameters.Count == 0
            || !TryGetContract(semanticModel, methodDeclaration, context.CancellationToken, out var contract))
        {
            return;
        }

        context.RegisterCodeFix(
            CodeAction.Create(
                Title,
                c => ChangeKeyParameterAsync(context.Document, methodDeclaration, contract, c),
                Title),
            context.Diagnostics);
    }

    private static async Task<Document> ChangeKeyParameterAsync(
        Document document,
        MethodDeclarationSyntax methodDeclaration,
        DataLoaderContract contract,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        if (root is null)
        {
            return document;
        }

        var parameter = methodDeclaration.ParameterList.Parameters[0];
        var typeName = contract.Kind is DataLoaderKind.Batch
            ? $"global::System.Collections.Generic.IReadOnlyList<{GetTypeName(contract.KeyType)}>"
            : GetTypeName(contract.KeyType);
        var newParameter = parameter.WithType(
            SyntaxFactory.ParseTypeName(typeName).WithTriviaFrom(parameter.Type!));
        var newRoot = root.ReplaceNode(parameter, newParameter);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool TryGetContract(
        SemanticModel semanticModel,
        MethodDeclarationSyntax methodDeclaration,
        CancellationToken cancellationToken,
        out DataLoaderContract contract)
    {
        var attribute = GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
                semanticModel,
                methodDeclaration,
                semanticModel.Compilation,
                cancellationToken)
            .LastOrDefault(t => t.IsGeneric);

        if (attribute.IsGeneric
            && attribute.Type.TypeArguments[0] is INamedTypeSymbol dataLoaderType
            && GenericDataLoaderAnalyzerHelper.TryResolveContract(
                dataLoaderType,
                semanticModel.Compilation,
                out contract))
        {
            return true;
        }

        contract = default;
        return false;
    }

    private static string GetTypeName(ITypeSymbol type)
        => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
}
