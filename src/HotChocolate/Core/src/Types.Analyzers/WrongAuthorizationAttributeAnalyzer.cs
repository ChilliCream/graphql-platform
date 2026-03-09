using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class WrongAuthorizationAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.WrongAuthorizeAttribute];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeClassDeclaration, SyntaxKind.ClassDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeRecordDeclaration, SyntaxKind.RecordDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;

        // Get the containing type (class or record)
        var containingType = methodDeclaration.Parent as TypeDeclarationSyntax;
        if (containingType is null)
        {
            return;
        }

        // Check if the type has ObjectType or root type attribute
        if (!HasGraphQLTypeAttribute(context, containingType))
        {
            return;
        }

        AnalyzeAttributesForMember(methodDeclaration.AttributeLists, context);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        // Get the containing type (class or record)
        var containingType = propertyDeclaration.Parent as TypeDeclarationSyntax;
        if (containingType is null)
        {
            return;
        }

        // Check if the type has ObjectType or root type attribute
        if (!HasGraphQLTypeAttribute(context, containingType))
        {
            return;
        }

        AnalyzeAttributesForMember(propertyDeclaration.AttributeLists, context);
    }

    private static void AnalyzeClassDeclaration(SyntaxNodeAnalysisContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;

        // Check if the class has ObjectType or root type attribute
        if (!HasGraphQLTypeAttribute(context, classDeclaration))
        {
            return;
        }

        AnalyzeAttributesForMember(classDeclaration.AttributeLists, context);
    }

    private static void AnalyzeRecordDeclaration(SyntaxNodeAnalysisContext context)
    {
        var recordDeclaration = (RecordDeclarationSyntax)context.Node;

        // Check if the record has ObjectType or root type attribute
        if (!HasGraphQLTypeAttribute(context, recordDeclaration))
        {
            return;
        }

        AnalyzeAttributesForMember(recordDeclaration.AttributeLists, context);
    }

    private static void AnalyzeAttributesForMember(
        SyntaxList<AttributeListSyntax> attributeLists,
        SyntaxNodeAnalysisContext context)
    {
        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                if (IsMicrosoftAuthorizationAttribute(attributeType))
                {
                    var diagnostic = Diagnostic.Create(
                        Errors.WrongAuthorizeAttribute,
                        attribute.GetLocation(),
                        attributeType.Name);

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool HasGraphQLTypeAttribute(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration)
    {
        return HasObjectTypeAttribute(context, typeDeclaration)
            || HasRootTypeAttribute(context, typeDeclaration);
    }

    private static bool HasObjectTypeAttribute(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration)
    {
        foreach (var attributeList in typeDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType;

                // Check if this is ObjectTypeAttribute
                if (attributeType is INamedTypeSymbol namedAttributeType
                    && namedAttributeType.Name == "ObjectTypeAttribute"
                    && namedAttributeType.ContainingNamespace?.ToDisplayString() == "HotChocolate.Types")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasRootTypeAttribute(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration)
    {
        if (typeDeclaration.AttributeLists.Count == 0)
        {
            return false;
        }

        foreach (var attributeList in typeDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType.ToDisplayString();

                if (attributeType.Equals(QueryTypeAttribute, StringComparison.Ordinal)
                    || attributeType.Equals(MutationTypeAttribute, StringComparison.Ordinal)
                    || attributeType.Equals(SubscriptionTypeAttribute, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsMicrosoftAuthorizationAttribute(INamedTypeSymbol attributeType)
    {
        var attributeName = attributeType.Name;
        var namespaceName = attributeType.ContainingNamespace?.ToDisplayString();

        if (namespaceName != "Microsoft.AspNetCore.Authorization")
        {
            return false;
        }

        return attributeName is "AuthorizeAttribute" or "AllowAnonymousAttribute";
    }
}
