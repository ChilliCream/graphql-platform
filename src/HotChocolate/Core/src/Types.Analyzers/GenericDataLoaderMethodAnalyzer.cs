using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Inspectors;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class GenericDataLoaderMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.DataLoaderCannotBeGeneric, Errors.MethodAccessModifierInvalid];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(
            methodDeclaration,
            context.CancellationToken);

        if (methodSymbol is null)
        {
            return;
        }

        var attributes = GenericDataLoaderAnalyzerHelper.GetDataLoaderAttributes(
            context.SemanticModel,
            methodDeclaration,
            context.Compilation,
            context.CancellationToken);

        if (!attributes.Any(t => t.IsGeneric))
        {
            return;
        }

        var location = Location.Create(methodDeclaration.SyntaxTree, methodDeclaration.Modifiers.Span);

        if (methodSymbol.IsGenericMethod)
        {
            context.ReportDiagnostic(Diagnostic.Create(Errors.DataLoaderCannotBeGeneric, location));
        }

        if (methodSymbol.DeclaredAccessibility is not (
            Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedAndInternal))
        {
            context.ReportDiagnostic(Diagnostic.Create(Errors.MethodAccessModifierInvalid, location));
        }
    }
}
