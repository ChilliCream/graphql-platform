using System.Globalization;

namespace HotChocolate;

public static class ErrorBuilderExtensions
{
    public static IErrorBuilder SetMessage(this IErrorBuilder builder, string format, params object[] args)
        => builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, args));
}
