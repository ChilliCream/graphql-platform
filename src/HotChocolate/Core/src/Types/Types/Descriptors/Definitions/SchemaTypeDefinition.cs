using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable  enable

namespace HotChocolate.Types.Descriptors.Definitions;

public class SchemaTypeDefinition
    : DefinitionBase<SchemaDefinitionNode>
    , IHasDirectiveDefinition
{
    private List<DirectiveDefinition>? _directives;

    /// <summary>
    /// Gets the list of directives that are annotated to this schema.
    /// </summary>
    public IList<DirectiveDefinition> Directives =>
        _directives ??= new List<DirectiveDefinition>();

    /// <summary>
    /// Specifies if this schema has directives.
    /// </summary>
    public bool HasDirectives => _directives is { Count: > 0 };

    /// <summary>
    /// Gets the list of directives that are annotated to this schema.
    /// </summary>
    public IReadOnlyList<DirectiveDefinition> GetDirectives()
    {
        if (_directives is null)
        {
            return Array.Empty<DirectiveDefinition>();
        }

        return _directives;
    }
}
