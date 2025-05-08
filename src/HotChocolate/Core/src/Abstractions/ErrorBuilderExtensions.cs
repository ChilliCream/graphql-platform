using System.Globalization;

namespace HotChocolate;

public static class ErrorBuilderExtensions
{
    public static ErrorBuilder SetFieldCoordinate(
        this ErrorBuilder builder,
        SchemaCoordinate fieldCoordinate)
        => builder.SetExtension(nameof(fieldCoordinate), fieldCoordinate.ToString());

    public static ErrorBuilder SetMessage(this ErrorBuilder builder, string format, params object[] args)
        => builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, args));
}
