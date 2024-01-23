using System;
using System.Collections.Generic;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public class SchemaTypeDefinition
    : DefinitionBase<SchemaDefinitionNode>
{
    private List<DirectiveDefinition>? _directives;

    /// <summary>
    /// Gets the list of directives that are annotated to this schema.
    /// </summary>
    internal IList<DirectiveDefinition> Directives =>
        _directives ??= [];

    /// <summary>
    /// Specifies if this schema has directives.
    /// </summary>
    internal bool HasDirectives => _directives is { Count: > 0, };

    /// <summary>
    /// Gets the list of directives that are annotated to this schema.
    /// </summary>
    internal IReadOnlyList<DirectiveDefinition> GetDirectives()
    {
        if (_directives is null)
        {
            return Array.Empty<DirectiveDefinition>();
        }

        return _directives;
    }
    
    internal IHasDirectiveDefinition GetLegacyDefinition() 
        => new CompatibilityLayer(this);

    private class CompatibilityLayer(SchemaTypeDefinition definition) : IHasDirectiveDefinition
    {
        public bool HasDirectives => definition.HasDirectives;

        public IList<DirectiveDefinition> Directives => definition.Directives;

        public IReadOnlyList<DirectiveDefinition> GetDirectives()
            => definition.GetDirectives();
    }
}
