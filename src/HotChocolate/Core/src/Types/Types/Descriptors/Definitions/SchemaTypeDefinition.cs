using HotChocolate.Features;

#nullable enable

namespace HotChocolate.Types.Descriptors.Definitions;

public class SchemaTypeDefinition : DefinitionBase, IFeatureProvider
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

    public IFeatureCollection Features { get; } = new FeatureCollection();

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
