using System.Diagnostics.Contracts;
using System.Collections.Specialized;
using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge;

public class SchemaMergeContext
    : ISchemaMergeContext
{
    private readonly Dictionary<string, ITypeDefinitionNode> _types = new();
    private readonly Dictionary<string, DirectiveDefinitionNode> _dirs = new();

    public void AddType(ITypeDefinitionNode type)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (_types.ContainsKey(type.Name.Value))
        {
            throw new ArgumentException(
                "A type with that name was already added.");
        }

        _types.Add(type.Name.Value, type);
    }

    public void AddDirective(DirectiveDefinitionNode directive)
    {
        if (directive == null)
        {
            throw new ArgumentNullException(nameof(directive));
        }

        if (_dirs.ContainsKey(directive.Name.Value))
        {
            throw new ArgumentException(
                "A type with that name was already added.");
        }

        _dirs.Add(directive.Name.Value, directive);
    }

    public bool ContainsType(string typeName)
    {
        typeName.EnsureGraphQLName(nameof(typeName));
        return _types.ContainsKey(typeName);
    }

    public bool ContainsDirective(string directiveName)
    {
        directiveName.EnsureGraphQLName(nameof(directiveName));
        return _dirs.ContainsKey(directiveName);
    }

    public DocumentNode CreateSchema()
    {
        var definitions = new List<IDefinitionNode>();
        definitions.AddRange(_types.Values);
        definitions.AddRange(_dirs.Values);
        return new DocumentNode(definitions);
    }
}
