using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers.Inspectors;

public sealed class ModuleInspector : ISyntaxInspector
{
    public ImmutableArray<ISyntaxFilter> Filters { get; } = [AssemblyAttributeList.Instance];

    public IImmutableSet<SyntaxKind> SupportedKinds { get; } = [SyntaxKind.AttributeList];

    public bool TryHandle(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        if (context.Node is AttributeListSyntax attributeList)
        {
            foreach (var attributeSyntax in attributeList.Attributes)
            {
                var symbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol;
                if (symbol is not IMethodSymbol attributeSymbol)
                {
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                if (fullName.Equals(ModuleAttribute, Ordinal)
                    && attributeSyntax.ArgumentList is { Arguments.Count: > 0 })
                {
                    var nameExpr = attributeSyntax.ArgumentList.Arguments[0].Expression;
                    var name = context.SemanticModel.GetConstantValue(nameExpr).ToString();

                    var features = (int)ModuleOptions.Default;
                    if (attributeSyntax.ArgumentList.Arguments.Count > 1)
                    {
                        var featuresExpr = attributeSyntax.ArgumentList.Arguments[1].Expression;
                        features = (int)context.SemanticModel.GetConstantValue(featuresExpr).Value!;
                    }

                    syntaxInfo = new ModuleInfo(name, (ModuleOptions)features);
                    return true;
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}
