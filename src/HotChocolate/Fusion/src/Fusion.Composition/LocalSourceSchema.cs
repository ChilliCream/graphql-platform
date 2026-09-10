using System.Text.Json;

namespace HotChocolate.Fusion;

/// <summary>
/// Represents a source schema that the local environment contributes to a composition.
/// </summary>
internal readonly record struct LocalSourceSchema
{
    /// <summary>
    /// Initializes a new instance of <see cref="LocalSourceSchema"/>. Throws
    /// <see cref="ArgumentException"/> when <paramref name="urlOverride"/> is a relative URI.
    /// </summary>
    public LocalSourceSchema(SourceSchemaText schema, JsonDocument settings, Uri? urlOverride)
    {
        if (urlOverride is { IsAbsoluteUri: false })
        {
            throw new ArgumentException(
                "The URL override must be an absolute URI.",
                nameof(urlOverride));
        }

        Schema = schema;
        Settings = settings;
        UrlOverride = urlOverride;
    }

    /// <summary>
    /// Gets the source schema document.
    /// </summary>
    public SourceSchemaText Schema { get; }

    /// <summary>
    /// Gets the settings document of the source schema.
    /// </summary>
    public JsonDocument Settings { get; }

    /// <summary>
    /// Gets the absolute URL that replaces the configured HTTP transport URL of the source
    /// schema. When it is <c>null</c>, no override applies and the configured URLs are used.
    /// </summary>
    public Uri? UrlOverride { get; }
}
