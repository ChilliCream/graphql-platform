using System.Text.RegularExpressions;
using HotChocolate.Utilities;

namespace HotChocolate.Types.Descriptors.Configurations;

public partial class SchemaTypeConfiguration : TypeSystemConfiguration
{
    private List<DirectiveConfiguration>? _directives;

    [GeneratedRegex(@"^[A-Za-z_][A-Za-z0-9_-]*$", RegexOptions.Compiled)]
    private static partial Regex NameValidationRegex();

    public override string Name
    {
        get;
        set
        {
            if (!string.IsNullOrEmpty(value) && !NameValidationRegex().IsMatch(value))
            {
                throw new ArgumentException(
                    "The schema name must start with a letter or underscore, "
                    + "followed by letters, digits, underscores, or hyphens.",
                    nameof(value));
            }

            field = string.Intern(value);
        }
    } = string.Empty;

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
