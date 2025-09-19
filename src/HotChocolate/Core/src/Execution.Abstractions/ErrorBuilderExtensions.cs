using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate;

/// <summary>
/// Provides extension methods for <see cref="ErrorBuilder"/>.
/// </summary>
public static class ErrorBuilderExtensions
{
    /// <summary>
    /// Sets the field coordinate of the error.
    /// </summary>
    /// <param name="builder">The error builder.</param>
    /// <param name="fieldCoordinate">The field coordinate.</param>
    /// <returns>The error builder.</returns>
    public static ErrorBuilder SetFieldCoordinate(
        this ErrorBuilder builder,
        SchemaCoordinate fieldCoordinate)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.SetExtension(nameof(fieldCoordinate), fieldCoordinate.ToString());
    }

    /// <summary>
    /// Sets the message of the error.
    /// </summary>
    /// <param name="builder">The error builder.</param>
    /// <param name="format">The format of the message.</param>
    /// <param name="args">The arguments for the message.</param>
    /// <returns>The error builder.</returns>
    public static ErrorBuilder SetMessage(this ErrorBuilder builder, string format, params object[] args)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(format);
        ArgumentNullException.ThrowIfNull(args);

        return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, args));
    }

    /// <summary>
    /// Adds a location to the error.
    /// </summary>
    /// <param name="builder">The error builder.</param>
    /// <param name="node">The syntax node.</param>
    /// <returns>The error builder.</returns>
    public static ErrorBuilder AddLocation(this ErrorBuilder builder, ISyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Location is null)
        {
            return builder;
        }

        builder.AddLocation(new Location(node.Location.Line, node.Location.Column));
        return builder;
    }

    /// <summary>
    /// Adds multiple locations to the error.
    /// </summary>
    /// <param name="builder">The error builder.</param>
    /// <param name="nodes">The syntax nodes.</param>
    /// <returns>The error builder.</returns>
    public static ErrorBuilder AddLocations(this ErrorBuilder builder, IEnumerable<ISyntaxNode> nodes)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(nodes);

        foreach (var node in nodes)
        {
            if (node.Location is null)
            {
                continue;
            }

            builder.AddLocation(new Location(node.Location.Line, node.Location.Column));
        }

        return builder;
    }
}
