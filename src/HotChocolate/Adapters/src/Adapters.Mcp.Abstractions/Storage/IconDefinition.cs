using System.Text.RegularExpressions;

namespace HotChocolate.Adapters.Mcp.Storage;

/// <summary>
/// Represents an icon that can be used to visually identify a tool or prompt.
/// </summary>
public sealed partial class IconDefinition
{
    /// <summary>
    /// Represents an icon that can be used to visually identify a tool or prompt.
    /// </summary>
    /// <param name="source">
    /// The URI pointing to the icon resource. This can be an HTTP/HTTPS URL pointing to an image
    /// file or a data URI with base64-encoded image data.
    /// </param>
    public IconDefinition(Uri source)
    {
        Source = source;
    }

    /// <summary>
    /// The URI pointing to the icon resource. This can be an HTTP/HTTPS URL pointing to an image
    /// file or a data URI with base64-encoded image data.
    /// </summary>
    public Uri Source
    {
        get;
        private set
        {
            if (value.Scheme != Uri.UriSchemeHttp
                && value.Scheme != Uri.UriSchemeHttps
                && value.Scheme != "data")
            {
                throw new ArgumentException(
                    "The icon source URI must use the HTTP, HTTPS, or data scheme.",
                    nameof(Source));
            }

            field = value;
        }
    }

    /// <summary>
    /// The optional MIME type of the icon. This can be used to override the server's MIME type if
    /// it's missing or generic. Common values include "image/png", "image/jpeg", "image/svg+xml",
    /// and "image/webp".
    /// </summary>
    public string? MimeType
    {
        get;
        init
        {
            if (value?.Contains('/') == false)
            {
                throw new ArgumentException(
                    "The MIME type must be a valid type/subtype string.",
                    nameof(MimeType));
            }

            field = value;
        }
    }

    /// <summary>
    /// The optional size specifications for the icon. This can specify one or more sizes at which
    /// the icon file can be used. Examples include "48x48", or "any" for scalable formats like SVG.
    /// </summary>
    public IList<string>? Sizes
    {
        get;
        init
        {
            if (value?.Any(size => !IconSizeRegex().IsMatch(size)) == true)
            {
                throw new ArgumentException(
                    "Each size must have the format {WIDTH}x{HEIGHT} (e.g., 48x48) or be 'any'.",
                    nameof(Sizes));
            }

            field = value;
        }
    }

    /// <summary>
    /// The optional theme for this icon. Can be "light" or "dark". Used to specify which UI theme
    /// the icon is designed for.
    /// </summary>
    public string? Theme
    {
        get;
        init
        {
            if (value is not null and not "light" and not "dark")
            {
                throw new ArgumentException(
                    "The icon theme must be 'light' or 'dark'.",
                    nameof(Theme));
            }

            field = value;
        }
    }

    [GeneratedRegex(@"^([0-9]+x[0-9]+|any)\z")]
    private static partial Regex IconSizeRegex();
}
