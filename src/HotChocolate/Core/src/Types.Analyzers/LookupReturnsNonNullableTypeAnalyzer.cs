using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LookupReturnsNonNullableTypeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.LookupReturnsNonNullableType];

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

        var returnType = UnwrapTaskType(methodSymbol.ReturnType);

        if (IsNullableType(returnType))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Errors.LookupReturnsNonNullableType,
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

        var propertyType = UnwrapTaskType(propertySymbol.Type);

        if (IsNullableType(propertyType))
        {
            return;
        }

        var diagnostic = Diagnostic.Create(
            Errors.LookupReturnsNonNullableType,
            propertyDeclaration.Type.GetLocation());

        context.ReportDiagnostic(diagnostic);
    }

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

    private static ITypeSymbol UnwrapTaskType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.TypeArguments.Length == 1
            && namedType.Name is nameof(Task) or nameof(ValueTask))
        {
            return namedType.TypeArguments[0];
        }

        return typeSymbol;
    }

    private static bool IsNullableType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return true;
        }

        if (typeSymbol is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
        {
            return true;
        }

        return false;
    }
}
