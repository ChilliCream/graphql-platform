using System.Globalization;

namespace Mocha;

/// <summary>
/// The text form a header value takes on the wire, shared by every transport that carries one as text.
/// </summary>
internal static class HeaderValueText
{
    /// <summary>
    /// The string the URI was built from, escapes intact.
    /// </summary>
    public static string From(Uri value)
        => value.OriginalString;

    public static string From(TimeSpan value)
        => value.ToString("c", CultureInfo.InvariantCulture);

    public static string From(DateOnly value)
        => value.ToString("O", CultureInfo.InvariantCulture);

    public static string From(TimeOnly value)
        => value.ToString("O", CultureInfo.InvariantCulture);

    public static string From(Enum value)
        => value.ToString();
}
