using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParentAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.ParentAttributeTypeMismatch];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get the containing class
        if (methodDeclaration.Parent is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        // Find the ObjectType<T> attribute on the class
        var objectTypeGenericArg = GetObjectTypeGenericArgument(classDeclaration, semanticModel);
        if (objectTypeGenericArg is null)
        {
            return;
        }

        // Check each parameter for [Parent] attribute
        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (parameter.AttributeLists.Count == 0)
            {
                continue;
            }

            var hasParentAttribute = false;
            foreach (var attributeList in parameter.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(attribute);
                    if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeType = attributeSymbol.ContainingType;
                    if (attributeType.Name == "ParentAttribute"
                        && attributeType.ContainingNamespace?.ToDisplayString() == "HotChocolate")
                    {
                        hasParentAttribute = true;
                        break;
                    }
                }

                if (hasParentAttribute)
                {
                    break;
                }
            }

            if (!hasParentAttribute)
            {
                continue;
            }

            // Get the parameter type
            var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter);
            if (parameterSymbol?.Type is not ITypeSymbol parameterType)
            {
                continue;
            }

            // Check if the parameter type is compatible with the ObjectType generic argument
            // Valid if:
            // 1. parameterType == objectTypeGenericArg
            // 2. objectTypeGenericArg inherits from parameterType
            // 3. objectTypeGenericArg implements parameterType (if it's an interface)
            if (!IsValidParentType(objectTypeGenericArg, parameterType))
            {
                var diagnostic = Diagnostic.Create(
                    Errors.ParentAttributeTypeMismatch,
                    parameter.Type?.GetLocation() ?? parameter.GetLocation(),
                    parameterType.Name,
                    objectTypeGenericArg.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
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

    private static bool IsValidParentType(ITypeSymbol objectTypeGenericArg, ITypeSymbol parameterType)
    {
        // Check if types are equal
        if (SymbolEqualityComparer.Default.Equals(objectTypeGenericArg, parameterType))
        {
            return true;
        }

        // Check if objectTypeGenericArg inherits from parameterType or implements it
        // This means parameterType is a base class or interface of objectTypeGenericArg
        var currentType = objectTypeGenericArg;

        // Check base classes
        while (currentType?.BaseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType.BaseType, parameterType))
            {
                return true;
            }
            currentType = currentType.BaseType;
        }

        // Check interfaces
        foreach (var iface in objectTypeGenericArg.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, parameterType))
            {
                return true;
            }
        }

        return false;
    }
}
