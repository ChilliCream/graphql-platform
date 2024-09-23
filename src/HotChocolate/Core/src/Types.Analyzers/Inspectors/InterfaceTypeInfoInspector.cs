using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Filters;
using HotChocolate.Types.Analyzers.Helpers;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Inspectors;

public class InterfaceTypeInfoInspector : ISyntaxInspector
{
    public IReadOnlyList<ISyntaxFilter> Filters => [TypeWithAttribute.Instance];

    public bool TryHandle(GeneratorSyntaxContext context, [NotNullWhen(true)] out SyntaxInfo? syntaxInfo)
    {
        var diagnostics = ImmutableArray<Diagnostic>.Empty;

        if (!IsInterfaceType(context, out var possibleType, out var classSymbol, out var runtimeType))
        {
            syntaxInfo = null;
            return false;
        }

        if (!possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
        {
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.InterfaceTypePartialKeywordMissing,
                    Location.Create(possibleType.SyntaxTree, possibleType.Span)));
        }

        if (!possibleType.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            diagnostics = diagnostics.Add(
                Diagnostic.Create(
                    Errors.InterfaceTypeStaticKeywordMissing,
                    Location.Create(possibleType.SyntaxTree, possibleType.Span)));
        }

        var members = classSymbol.GetMembers();
        var resolvers = new Resolver[members.Length];
        var i = 0;

        foreach (var member in members)
        {
            if (member.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal)
            {
                if (member is IMethodSymbol { MethodKind: MethodKind.Ordinary } methodSymbol)
                {
                    resolvers[i++] = CreateResolver(context, classSymbol, methodSymbol);
                    continue;
                }

                if (member is IPropertySymbol)
                {
                    resolvers[i++] = new Resolver(
                        classSymbol.Name,
                        member,
                        ResolverResultKind.Pure,
                        ImmutableArray<ResolverParameter>.Empty,
                        ImmutableArray<MemberBinding>.Empty);
                }
            }
        }

        if (i > 0 && i < resolvers.Length)
        {
            Array.Resize(ref resolvers, i);
        }

        syntaxInfo = new InterfaceTypeExtensionInfo(
            classSymbol,
            runtimeType,
            possibleType,
            i == 0
                ? ImmutableArray<Resolver>.Empty
                : resolvers.ToImmutableArray());

        if (diagnostics.Length > 0)
        {
            syntaxInfo.AddDiagnosticRange(diagnostics);
        }

        return true;
    }

    private static bool IsInterfaceType(
        GeneratorSyntaxContext context,
        [NotNullWhen(true)] out ClassDeclarationSyntax? resolverTypeSyntax,
        [NotNullWhen(true)] out INamedTypeSymbol? resolverTypeSymbol,
        [NotNullWhen(true)] out INamedTypeSymbol? runtimeType)
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

                    // We do a start with here to capture the generic and non-generic variant of
                    // the object type extension attribute.
                    if (fullName.StartsWith(WellKnownAttributes.InterfaceTypeAttribute, StringComparison.Ordinal)
                        && attributeContainingTypeSymbol.TypeArguments.Length == 1
                        && attributeContainingTypeSymbol.TypeArguments[0] is INamedTypeSymbol rt
                        && ModelExtensions.GetDeclaredSymbol(context.SemanticModel, possibleType) is INamedTypeSymbol rts)
                    {
                        resolverTypeSyntax = possibleType;
                        resolverTypeSymbol = rts;
                        runtimeType = rt;
                        return true;
                    }
                }
            }
        }

        resolverTypeSyntax = null;
        resolverTypeSymbol = null;
        runtimeType = null;
        return false;
    }

    private static Resolver CreateResolver(
        GeneratorSyntaxContext context,
        INamedTypeSymbol resolverType,
        IMethodSymbol resolverMethod)
    {
        var compilation = context.SemanticModel.Compilation;
        var parameters = resolverMethod.Parameters;
        var resolverParameters = new ResolverParameter[parameters.Length];

        for (var i = 0; i < parameters.Length; i++)
        {
            resolverParameters[i] = ResolverParameter.Create(parameters[i], compilation);
        }

        return new Resolver(
            resolverType.Name,
            resolverMethod,
            resolverMethod.GetResultKind(),
            resolverParameters.ToImmutableArray(),
            ImmutableArray<MemberBinding>.Empty);
    }
}
