using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class QueryContextConnectionAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.QueryContextConnectionMismatch];

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

        // Check if class has one of the target attributes
        if (!HasRelevantTypeAttribute(classDeclaration, semanticModel))
        {
            return;
        }

        // Find QueryContext<T> parameter
        ParameterSyntax? queryContextParameter = null;
        ITypeSymbol? queryContextGenericArg = null;

        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (parameter.Type is null)
            {
                continue;
            }

            var typeInfo = semanticModel.GetTypeInfo(parameter.Type);
            if (typeInfo.Type is INamedTypeSymbol namedType && IsQueryContext(namedType))
            {
                queryContextParameter = parameter;
                if (namedType.TypeArguments.Length == 1)
                {
                    queryContextGenericArg = namedType.TypeArguments[0];
                }
                break;
            }
        }

        if (queryContextParameter is null || queryContextGenericArg is null)
        {
            return;
        }

        // Get return type and check if it implements IConnection<TNode>
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol is null)
        {
            return;
        }

        var returnType = methodSymbol.ReturnType;
        var connectionNodeType = GetConnectionNodeType(returnType);

        if (connectionNodeType is null)
        {
            return;
        }

        // Compare the generic types
        if (!SymbolEqualityComparer.Default.Equals(queryContextGenericArg, connectionNodeType))
        {
            var diagnostic = Diagnostic.Create(
                Errors.QueryContextConnectionMismatch,
                queryContextParameter.Type?.GetLocation() ?? queryContextParameter.GetLocation(),
                queryContextGenericArg.Name,
                connectionNodeType.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasRelevantTypeAttribute(
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
                if (attributeType is not INamedTypeSymbol namedAttributeType)
                {
                    continue;
                }

                var namespaceName = namedAttributeType.ContainingNamespace?.ToDisplayString();
                if (namespaceName != "HotChocolate.Types")
                {
                    continue;
                }

                // Check for ObjectTypeAttribute, InterfaceTypeAttribute, QueryTypeAttribute,
                // MutationTypeAttribute, or SubscriptionTypeAttribute
                if (namedAttributeType.Name is "ObjectTypeAttribute"
                    or "InterfaceTypeAttribute"
                    or "QueryTypeAttribute"
                    or "MutationTypeAttribute"
                    or "SubscriptionTypeAttribute")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsQueryContext(INamedTypeSymbol type)
    {
        // Check if this is QueryContext<T>
        if (type.Name != "QueryContext")
        {
            return false;
        }

        var namespaceName = type.ContainingNamespace?.ToDisplayString();
        return namespaceName == "GreenDonut.Data";
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
