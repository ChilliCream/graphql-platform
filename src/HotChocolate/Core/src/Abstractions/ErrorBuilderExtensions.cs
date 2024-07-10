using System.Globalization;

namespace HotChocolate;

public static class ErrorBuilderExtensions
{
    public static IErrorBuilder SetFieldCoordinate(
        this IErrorBuilder builder,
        SchemaCoordinate fieldCoordinate)
        => builder.SetExtension(nameof(fieldCoordinate), fieldCoordinate.ToString());

    public static IErrorBuilder SetMessage(this IErrorBuilder builder, string format, params object[] args)
        => builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, args));
}
