using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HotChocolate.Types.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class IdAttributeOnRecordParameterAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        [Errors.IdAttributeOnRecordParameter];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeParameter, SyntaxKind.Parameter);
    }

    private static void AnalyzeParameter(SyntaxNodeAnalysisContext context)
    {
        var parameter = (ParameterSyntax)context.Node;

        // Check if the parameter is in a record declaration
        if (parameter.Parent?.Parent is not RecordDeclarationSyntax)
        {
            return;
        }

        // Check if the parameter has attributes
        if (parameter.AttributeLists.Count == 0)
        {
            return;
        }

        // Look for [ID] attribute without property: target
        foreach (var attributeList in parameter.AttributeLists)
        {
            // Skip attributes that already have a target (like [property: ID])
            if (attributeList.Target is not null)
            {
                continue;
            }

            foreach (var attribute in attributeList.Attributes)
            {
                var attributeType = context.SemanticModel.GetTypeInfo(attribute).Type;
                if (attributeType is null)
                {
                    continue;
                }

                // Check if this is the ID attribute from HotChocolate.Types.Relay
                if (attributeType.Name == "IDAttribute"
                    && attributeType.ContainingNamespace?.ToDisplayString() == "HotChocolate.Types.Relay")
                {
                    // Report diagnostic - ID attribute on record parameter without property: target
                    context.ReportDiagnostic(
                        Diagnostic.Create(
                            Errors.IdAttributeOnRecordParameter,
                            attribute.GetLocation()));
                }
            }
        }
    }
}
