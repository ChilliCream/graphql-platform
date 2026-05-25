using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class BindMemberAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.BindMemberNotFound, Errors.BindMemberTypeMismatch];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classDeclaration)
        {
            return;
        }

        // Get the ObjectType<T> attribute and extract T
        var objectTypeGenericArg = GetObjectTypeGenericArgument(context, classDeclaration);
        if (objectTypeGenericArg is null)
        {
            return;
        }

        // Analyze all methods in the class
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax methodDeclaration)
            {
                AnalyzeMethod(context, methodDeclaration, objectTypeGenericArg);
            }
        }
    }

    private static ITypeSymbol? GetObjectTypeGenericArgument(
        SyntaxNodeAnalysisContext context,
        ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.AttributeLists.Count == 0)
        {
            return null;
        }

        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                // Check if this is ObjectTypeAttribute (compare full name without generic args)
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

    private static void AnalyzeMethod(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration,
        ITypeSymbol objectTypeArg)
    {
        if (methodDeclaration.AttributeLists.Count == 0)
        {
            return;
        }

        foreach (var attributeList in methodDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType.ToDisplayString();
                if (!attributeType.Equals(BindMemberAttribute, StringComparison.Ordinal))
                {
                    continue;
                }

                // Get the argument passed to BindMember
                if (attribute.ArgumentList?.Arguments.Count > 0)
                {
                    var argument = attribute.ArgumentList.Arguments[0];
                    AnalyzeBindMemberArgument(context, argument, objectTypeArg, attribute);
                }
            }
        }
    }

    private static void AnalyzeBindMemberArgument(
        SyntaxNodeAnalysisContext context,
        AttributeArgumentSyntax argument,
        ITypeSymbol objectTypeArg,
        AttributeSyntax attribute)
    {
        if (argument.Expression is InvocationExpressionSyntax invocation)
        {
            // This is a nameof() expression
            AnalyzeNameofExpression(context, invocation, objectTypeArg, attribute);
        }
        else if (argument.Expression is LiteralExpressionSyntax literal
            && literal.IsKind(SyntaxKind.StringLiteralExpression))
        {
            // This is a string literal
            var memberName = literal.Token.ValueText;
            ValidateMemberExists(context, memberName, objectTypeArg, attribute);
        }
    }

    private static void AnalyzeNameofExpression(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation,
        ITypeSymbol objectTypeArg,
        AttributeSyntax attribute)
    {
        // Check if it's nameof
        if (invocation.Expression is not IdentifierNameSyntax { Identifier.Text: "nameof" })
        {
            return;
        }

        if (invocation.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        var nameofArgument = invocation.ArgumentList.Arguments[0].Expression;

        // Check if it's Type.Member format
        if (nameofArgument is MemberAccessExpressionSyntax memberAccess)
        {
            var typeInfo = context.SemanticModel.GetTypeInfo(memberAccess.Expression);
            if (typeInfo.Type is not null)
            {
                // Compare the type in nameof with the ObjectType's generic argument
                if (!SymbolEqualityComparer.Default.Equals(typeInfo.Type, objectTypeArg))
                {
                    var diagnostic = Diagnostic.Create(
                        Errors.BindMemberTypeMismatch,
                        memberAccess.Expression.GetLocation(),
                        typeInfo.Type.Name,
                        objectTypeArg.Name);

                    context.ReportDiagnostic(diagnostic);
                    return;
                }

                // Validate the member exists
                var memberName = memberAccess.Name.Identifier.Text;
                ValidateMemberExists(context, memberName, objectTypeArg, attribute);
            }
        }
        else if (nameofArgument is IdentifierNameSyntax identifier)
        {
            // Just a simple identifier, validate it exists on the type
            var memberName = identifier.Identifier.Text;
            ValidateMemberExists(context, memberName, objectTypeArg, attribute);
        }
    }

    private static void ValidateMemberExists(
        SyntaxNodeAnalysisContext context,
        string memberName,
        ITypeSymbol objectTypeArg,
        AttributeSyntax attribute)
    {
        // Check if the member exists on the type or any of its base types
        if (!HasMember(objectTypeArg, memberName))
        {
            var diagnostic = Diagnostic.Create(
                Errors.BindMemberNotFound,
                attribute.GetLocation(),
                memberName,
                objectTypeArg.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static bool HasMember(ITypeSymbol type, string memberName)
    {
        var current = type;

        while (current is not null)
        {
            if (!current.GetMembers(memberName).IsEmpty)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
