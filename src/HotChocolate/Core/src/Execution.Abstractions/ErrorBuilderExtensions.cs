using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate;

public static class ErrorBuilderExtensions
{
    public static ErrorBuilder SetFieldCoordinate(
        this ErrorBuilder builder,
        SchemaCoordinate fieldCoordinate)
        => builder.SetExtension(nameof(fieldCoordinate), fieldCoordinate.ToString());

    public static ErrorBuilder SetMessage(this ErrorBuilder builder, string format, params object[] args)
        => builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, args));

    public static ErrorBuilder AddLocation(this ErrorBuilder builder, ISyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if(node.Location is null)
        {
            return builder;
        }

        builder.AddLocation(new Location(node.Location.Line, node.Location.Column));
        return builder;
    }
}
