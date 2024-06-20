using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static System.StringComparison;
using static HotChocolate.Types.Analyzers.WellKnownAttributes;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class ObjectTypeExtensionInfoInspector : ISyntaxInspector
{
    public IReadOnlyList<ISyntaxFilter> Filters => [TypeWithAttribute.Instance];

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out ISyntaxInfo? syntaxInfo)
    {
        if (context.Node is ClassDeclarationSyntax { AttributeLists.Count: > 0, } possibleType)
        {
            foreach (var attributeListSyntax in possibleType.AttributeLists)
            {
                foreach (var attributeSyntax in attributeListSyntax.Attributes)
                {
                    var symbol = ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol;

                    if (symbol is not IMethodSymbol attributeSymbol)
                    {
                        continue;
                    }

                    var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                    var fullName = attributeContainingTypeSymbol.ToDisplayString();

                    // We do a start with here to capture the generic and non generic variant of
                    // the object type extension attribute.
                    if (fullName.StartsWith(ObjectTypeAttribute, Ordinal) &&
                        attributeContainingTypeSymbol.TypeArguments.Length == 1 &&
                        attributeContainingTypeSymbol.TypeArguments[0] is INamedTypeSymbol runtimeType &&
                        ModelExtensions.GetDeclaredSymbol(context.SemanticModel, possibleType) is INamedTypeSymbol
                            classSymbol)
                    {
                        var diagnostics = ImmutableArray<Diagnostic>.Empty;

                        if (!possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                        {
                            diagnostics = diagnostics.Add(
                                Diagnostic.Create(
                                    Errors.ObjectTypePartialKeywordMissing,
                                    Location.Create(possibleType.SyntaxTree, possibleType.Span)));
                        }

                        if (!possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
                        {
                            diagnostics = diagnostics.Add(
                                Diagnostic.Create(
                                    Errors.ObjectTypeStaticKeywordMissing,
                                    Location.Create(possibleType.SyntaxTree, possibleType.Span)));
                        }

                        var members = classSymbol.GetMembers();
                        var resolvers = new Resolver[members.Length];
                        IMethodSymbol? nodeResolver = null;
                        int i = 0;

                        foreach (var member in members)
                        {
                            if (member.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
                            {
                                if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol)
                                {
                                    if (methodSymbol.Skip())
                                    {
                                        continue;
                                    }

                                    if (methodSymbol.IsNodeResolver())
                                    {
                                        nodeResolver = methodSymbol;
                                    }
                                    else
                                    {
                                        var parameters = methodSymbol.Parameters;
                                        var resolverParameters = new ResolverParameter[parameters.Length];

                                        for (var j = 0; j < parameters.Length; j++)
                                        {
                                            resolverParameters[j] =
                                                ResolverParameter.Create(
                                                    parameters[j],
                                                    context.SemanticModel.Compilation);
                                        }

                                        resolvers[i++] = new Resolver(
                                            classSymbol.Name,
                                            methodSymbol,
                                            methodSymbol.GetResultKind(),
                                            resolverParameters.ToImmutableArray());
                                        continue;
                                    }
                                }

                                if (member is IPropertySymbol)
                                {
                                    resolvers[i++] = new Resolver(
                                        classSymbol.Name,
                                        member,
                                        ResolverResultKind.Pure,
                                        ImmutableArray<ResolverParameter>.Empty);
                                }
                            }
                        }

                        if (i > 0 && i < resolvers.Length)
                        {
                            Array.Resize(ref resolvers, i);
                        }

                        syntaxInfo = new ObjectTypeExtensionInfo(
                            classSymbol,
                            runtimeType,
                            nodeResolver,
                            diagnostics,
                            possibleType,
                            i == 0
                                ? ImmutableArray<Resolver>.Empty
                                : resolvers.ToImmutableArray());

                        return true;
                    }
                }
            }
        }

        syntaxInfo = null;
        return false;
    }
}

file static class Extensions
{
    public static bool IsNodeResolver(this IMethodSymbol methodSymbol)
        => methodSymbol
            .GetAttributes()
            .Any(t => t.AttributeClass?.ToDisplayString().Equals(NodeResolverAttribute, Ordinal) ?? false);

    public static bool Skip(this IMethodSymbol methodSymbol)
        => methodSymbol
            .GetAttributes()
            .Any(t =>
            {
                var name = t.AttributeClass?.ToDisplayString();

                if (name is null)
                {
                    return false;
                }

                if (name.Equals(DataLoaderAttribute, Ordinal) ||
                    name.Equals(QueryAttribute, Ordinal) ||
                    name.Equals(MutationAttribute, Ordinal) ||
                    name.Equals(SubscriptionAttribute, Ordinal))
                {
                    return true;
                }

                return false;
            });
}
