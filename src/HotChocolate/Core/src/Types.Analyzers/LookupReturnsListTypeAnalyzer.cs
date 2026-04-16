using System.Collections.Immutable;
using HotChocolate.Types.Analyzers.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LookupReturnsListTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.LookupReturnsListType];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        if (!HasLookupAttribute(context, methodDeclaration.AttributeLists))
        {
            return;
        }

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol is null)
        {
            return;
        }

        var returnType = context.Compilation.IsTaskOrValueTask(methodSymbol.ReturnType, out var innerType)
            ? innerType
            : methodSymbol.ReturnType;

        if (!IsListType(returnType))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Errors.LookupReturnsListType,
            methodDeclaration.ReturnType.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        if (!HasLookupAttribute(context, propertyDeclaration.AttributeLists))
        {
            return;
        }

        var propertySymbol = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
        if (propertySymbol is null)
        {
            return;
        }

        var propertyType = context.Compilation.IsTaskOrValueTask(propertySymbol.Type, out var innerType)
            ? innerType
            : propertySymbol.Type;

        if (!IsListType(propertyType))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Errors.LookupReturnsListType,
            propertyDeclaration.Type.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsListType(ITypeSymbol typeSymbol)
        => typeSymbol is IArrayTypeSymbol || typeSymbol.IsListType(out _);

    private static bool HasLookupAttribute(
        SyntaxNodeAnalysisContext context,
        SyntaxList<AttributeListSyntax> attributeLists)
    {
        var semanticModel = context.SemanticModel;

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;
                if (attributeType.ToDisplayString() == WellKnownAttributes.LookupAttribute)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
