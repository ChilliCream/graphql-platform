using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParentMethodAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.ParentMethodTypeMismatch];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var semanticModel = context.SemanticModel;

        // Get the class symbol
        var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);
        if (classSymbol is null)
        {
            return;
        }

        // Find the ObjectType<T> base type
        var objectTypeGenericArg = GetObjectTypeGenericArgument(classSymbol);
        if (objectTypeGenericArg is null)
        {
            return;
        }

        // Analyze all methods in the class for Parent<T>() calls
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                AnalyzeMethodBody(context, methodDeclaration, objectTypeGenericArg, semanticModel);
            }
        }
    }

    private static void AnalyzeMethodBody(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration,
        ITypeSymbol expectedType,
        SemanticModel semanticModel)
    {
        if (methodDeclaration.Body is null && methodDeclaration.ExpressionBody is null)
        {
            return;
        }

        // Find all invocation expressions in the method
        var invocations = methodDeclaration.DescendantNodes()
            .OfType<InvocationExpressionSyntax>();

        foreach (var invocation in invocations)
        {
            // Check if this is a Parent<T>() call
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            {
                continue;
            }

            // Check if the method name is "Parent"
            if (memberAccess.Name is not GenericNameSyntax genericName
                || genericName.Identifier.Text != "Parent")
            {
                continue;
            }

            // Get the method symbol
            var symbolInfo = semanticModel.GetSymbolInfo(invocation);
            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                continue;
            }

            // Verify this is the Parent method from IResolverContext
            if (!IsParentMethod(methodSymbol))
            {
                continue;
            }

            // Get the generic type argument
            if (genericName.TypeArgumentList.Arguments.Count != 1)
            {
                continue;
            }

            var typeArgument = genericName.TypeArgumentList.Arguments[0];
            var typeArgumentSymbol = semanticModel.GetTypeInfo(typeArgument).Type;
            if (typeArgumentSymbol is null)
            {
                continue;
            }

            // Check if the type argument is compatible with the expected type
            if (!IsValidParentType(expectedType, typeArgumentSymbol))
            {
                var diagnostic = Diagnostic.Create(
                    Errors.ParentMethodTypeMismatch,
                    typeArgument.GetLocation(),
                    typeArgumentSymbol.Name,
                    expectedType.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
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

    private static bool IsParentMethod(IMethodSymbol methodSymbol)
    {
        // Check if the method is named "Parent" and is a member of IResolverContext
        if (methodSymbol.Name != "Parent")
        {
            return false;
        }

        // Check if it's from IResolverContext or related interfaces
        var containingType = methodSymbol.ContainingType;
        if (containingType is null)
        {
            return false;
        }

        var containingTypeName = containingType.Name;
        var containingNamespace = containingType.ContainingNamespace?.ToDisplayString();

        return containingNamespace == "HotChocolate.Resolvers"
            && (containingTypeName is "IResolverContext" or "IMiddlewareContext");
    }

    private static bool IsValidParentType(ITypeSymbol expectedType, ITypeSymbol actualType)
    {
        // Check if types are equal
        if (SymbolEqualityComparer.Default.Equals(expectedType, actualType))
        {
            return true;
        }

        // Check if expectedType inherits from actualType or implements it
        // This means actualType is a base class or interface of expectedType
        var currentType = expectedType;

        // Check base classes
        while (currentType?.BaseType is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentType.BaseType, actualType))
            {
                return true;
            }
            currentType = currentType.BaseType;
        }

        // Check interfaces
        foreach (var iface in expectedType.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(iface, actualType))
            {
                return true;
            }
        }

        return false;
    }
}
