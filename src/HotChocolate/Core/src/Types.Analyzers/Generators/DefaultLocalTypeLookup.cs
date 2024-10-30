using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class DefaultLocalTypeLookup(ImmutableArray<SyntaxInfo> syntaxInfos) : ILocalTypeLookup
{
    private Dictionary<string, List<string>>? _typeNameLookup;
    private readonly ImmutableArray<SyntaxInfo> _syntaxInfos = syntaxInfos;

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
            _typeNameLookup = new Dictionary<string, List<string>>();
            foreach (var syntaxInfo in _syntaxInfos)
            {
                if(syntaxInfo is not DataLoaderInfo dataLoaderInfo)
                {
                    continue;
                }

                if (!_typeNameLookup.TryGetValue(dataLoaderInfo.Name, out var typeNames))
                {
                    typeNames = [];
                    _typeNameLookup[dataLoaderInfo.Name] = typeNames;
                }

                typeNames.Add("global::" + dataLoaderInfo.FullName);

                if (!_typeNameLookup.TryGetValue(dataLoaderInfo.InterfaceName, out typeNames))
                {
                    typeNames = [];
                    _typeNameLookup[dataLoaderInfo.InterfaceName] = typeNames;
                }

                typeNames.Add("global::" + dataLoaderInfo.InterfaceFullName);
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
