using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NodeResolverIdAttributeAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.NodeResolverIdAttributeNotAllowed];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
    }

    private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
        {
            return;
        }

        // Check if method has NodeResolver attribute
        if (!HasNodeResolverAttribute(context, methodDeclaration))
        {
            return;
        }

        // Check parameters for ID attribute
        foreach (var parameter in methodDeclaration.ParameterList.Parameters)
        {
            if (HasIdAttribute(context, parameter))
            {
                // Report diagnostic on the ID attribute
                var idAttribute = GetIdAttribute(context, parameter);
                if (idAttribute is not null)
                {
                    var diagnostic = Diagnostic.Create(
                        Errors.NodeResolverIdAttributeNotAllowed,
                        idAttribute.GetLocation());

                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }

    private static bool HasNodeResolverAttribute(
        SyntaxNodeAnalysisContext context,
        MethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration.AttributeLists.Count == 0)
        {
            return false;
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

                if (attributeType.Equals(NodeResolverAttribute, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool HasIdAttribute(
        SyntaxNodeAnalysisContext context,
        ParameterSyntax parameter)
    {
        if (parameter.AttributeLists.Count == 0)
        {
            return false;
        }

        foreach (var attributeList in parameter.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType.ToDisplayString();

                // Check for both IDAttribute and IDAttribute<T>
                if (attributeType.Equals("HotChocolate.Types.Relay.IDAttribute", StringComparison.Ordinal)
                    || attributeType.StartsWith("HotChocolate.Types.Relay.IDAttribute<", StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static AttributeSyntax? GetIdAttribute(
        SyntaxNodeAnalysisContext context,
        ParameterSyntax parameter)
    {
        if (parameter.AttributeLists.Count == 0)
        {
            return null;
        }

        foreach (var attributeList in parameter.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attribute);
                if (symbolInfo.Symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeType = attributeSymbol.ContainingType.ToDisplayString();

                // Check for both IDAttribute and IDAttribute<T>
                if (attributeType.Equals("HotChocolate.Types.Relay.IDAttribute", StringComparison.Ordinal)
                    || attributeType.StartsWith("HotChocolate.Types.Relay.IDAttribute<", StringComparison.Ordinal))
                {
                    return attribute;
                }
            }
        }

        return null;
    }
}
