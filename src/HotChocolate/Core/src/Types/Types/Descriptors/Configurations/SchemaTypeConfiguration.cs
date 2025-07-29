#nullable enable

namespace HotChocolate.Types.Descriptors.Configurations;

public class SchemaTypeConfiguration : TypeSystemConfiguration
{
    private List<DirectiveConfiguration>? _directives;

    /// <summary>
    /// Gets the list of directives that are annotated to this schema.
    /// </summary>
    internal IList<DirectiveConfiguration> Directives =>
        _directives ??= [];

    /// <summary>
    /// Specifies if this schema has directives.
    /// </summary>
    internal bool HasDirectives => _directives is { Count: > 0 };

    /// <summary>
    /// Gets the list of directives that are annotated to this schema.
    /// </summary>
    internal IReadOnlyList<DirectiveConfiguration> GetDirectives()
    {
        if (_directives is null)
        {
            return [];
        }

        return _directives;
    }

    internal IDirectiveConfigurationProvider GetLegacyConfiguration()
        => new CompatibilityLayer(this);

    private class CompatibilityLayer(SchemaTypeConfiguration definition) : IDirectiveConfigurationProvider
    {
        public bool HasDirectives => definition.HasDirectives;

        public IList<DirectiveConfiguration> Directives => definition.Directives;

        public IReadOnlyList<DirectiveConfiguration> GetDirectives()
            => definition.GetDirectives();
    }
}
