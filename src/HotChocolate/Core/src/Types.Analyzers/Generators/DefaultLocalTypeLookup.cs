using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types.Analyzers.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HotChocolate.Types.Analyzers.Generators;

public sealed class DefaultLocalTypeLookup(ImmutableArray<SyntaxInfo> syntaxInfos) : ILocalTypeLookup
{
#if NET9_0_OR_GREATER
    private readonly Lock _lock = new();
#else
    private readonly object _lock = new();
#endif
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

        foreach (var namespaceString in GetPotentialNamespaces(resolverMethod))
        {
            var candidateName = $"global::{namespaceString}.{type.Name}";
            if (typeNames.Contains(candidateName))
            {
                typeDisplayName = candidateName;
                return true;
            }
        }

        typeDisplayName = typeNames[0];
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

    private static IEnumerable<string> GetPotentialNamespaces(IMethodSymbol methodSymbol)
    {
        var root = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.SyntaxTree.GetRoot();

        if (root is not CompilationUnitSyntax compilationUnit)
        {
            return [];
        }

        var namespaces = new HashSet<string>();

        foreach (var member in compilationUnit.Members)
        {
            if (member is NamespaceDeclarationSyntax namespaceDeclaration)
            {
                namespaces.Add(namespaceDeclaration.Name.ToString());
            }
            else if (member is FileScopedNamespaceDeclarationSyntax fileScopedNamespace)
            {
                namespaces.Add(fileScopedNamespace.Name.ToString());
            }
        }

        return namespaces;
    }
}
