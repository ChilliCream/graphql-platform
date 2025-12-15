using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class DefaultLocalTypeLookup(ImmutableArray<SyntaxInfo> syntaxInfos) : ILocalTypeLookup
{
    private readonly object? _lock = new();
    private Dictionary<string, List<string>>? _typeNameLookup;

    public bool TryGetTypeName(
        ITypeSymbol type,
        IMethodSymbol resolverMethod,
        [NotNullWhen(true)] out string? typeDisplayName)
    {
        var typeNameLookup = GetTypeNameLookup();

        if (!typeNameLookup.TryGetValue(type.Name, out var typeNames))
        {
            typeDisplayName = null;
            return false;
        }

        if (typeNames.Count == 1)
        {
            typeDisplayName = typeNames[0];
            return true;
        }

        foreach (var namespaceString in GetContainingNamespaces(resolverMethod))
        {
            if (typeNames.Contains($"global::{namespaceString}.{type.Name}"))
            {
                typeDisplayName = typeNames[0];
                return true;
            }
        }

        typeDisplayName = type.Name;
        return true;
    }

    private Dictionary<string, List<string>> GetTypeNameLookup()
    {
        if (_typeNameLookup is null)
        {
            lock (_lock)
            {
                if (_typeNameLookup is null)
                {
                    var typeNameLookup = new Dictionary<string, List<string>>();

                    foreach (var syntaxInfo in syntaxInfos)
                    {
                        if (syntaxInfo is not DataLoaderInfo dataLoaderInfo)
                        {
                            continue;
                        }

                        if (!typeNameLookup.TryGetValue(dataLoaderInfo.Name, out var typeNames))
                        {
                            typeNames = [];
                            typeNameLookup[dataLoaderInfo.Name] = typeNames;
                        }

                        typeNames.Add("global::" + dataLoaderInfo.FullName);

                        if (!typeNameLookup.TryGetValue(dataLoaderInfo.InterfaceName, out typeNames))
                        {
                            typeNames = [];
                            typeNameLookup[dataLoaderInfo.InterfaceName] = typeNames;
                        }

                        typeNames.Add("global::" + dataLoaderInfo.InterfaceFullName);
                    }

                    _typeNameLookup = typeNameLookup;
                }
            }
        }

        return _typeNameLookup;
    }

    private static IEnumerable<string> GetContainingNamespaces(IMethodSymbol methodSymbol)
    {
        var namespaces = new HashSet<string>();
        var syntaxTree = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree;

        if (syntaxTree != null)
        {
            var root = syntaxTree.GetRoot();
            var namespaceDeclarations = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>();

            foreach (var namespaceDeclaration in namespaceDeclarations)
            {
                namespaces.Add(namespaceDeclaration.Name.ToString());
            }
        }

        return namespaces;
    }
}
