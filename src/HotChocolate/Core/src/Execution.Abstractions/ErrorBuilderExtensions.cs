using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using HotChocolate.Language;

namespace HotChocolate;

/// <summary>
/// Provides extension methods for <see cref="ErrorBuilder"/>.
/// </summary>
public static class ErrorBuilderExtensions
{
    extension(ErrorBuilder builder)
    {
        /// <summary>
        /// Sets the field coordinate of the error.
        /// </summary>
        /// <param name="coordinate">The field coordinate.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetCoordinate(SchemaCoordinate coordinate)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.SetExtension(nameof(coordinate), coordinate.ToString());
        }

        /// <summary>
        /// Sets the input path of the error.
        /// </summary>
        /// <param name="inputPath">The input path.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetInputPath(Path? inputPath)
        {
            if (inputPath is null)
            {
                return builder.RemoveExtension(nameof(inputPath));
            }
            else
            {
                return builder.SetExtension(nameof(inputPath), inputPath);
            }
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="arg1">The argument for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage([StringSyntax("CompositeFormat")] string format, object? arg1)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, arg1));
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="arg1">The first argument for the message.</param>
        /// <param name="arg2">The second argument for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage([StringSyntax("CompositeFormat")] string format, object? arg1, object? arg2)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, arg1, arg2));
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="arg1">The first argument for the message.</param>
        /// <param name="arg2">The second argument for the message.</param>
        /// <param name="arg3">The third argument for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage([StringSyntax("CompositeFormat")] string format, object? arg1, object? arg2, object? arg3)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, arg1, arg2, arg3));
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="arg1">The argument for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage([StringSyntax("CompositeFormat")] string format, string? arg1)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, arg1));
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="arg1">The first argument for the message.</param>
        /// <param name="arg2">The second argument for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage([StringSyntax("CompositeFormat")] string format, string? arg1, string? arg2)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, arg1, arg2));
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="arg1">The first argument for the message.</param>
        /// <param name="arg2">The second argument for the message.</param>
        /// <param name="arg3">The third argument for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage([StringSyntax("CompositeFormat")] string format, string? arg1, string? arg2, string? arg3)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, arg1, arg2, arg3));
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="args">The arguments for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage(
            [StringSyntax("CompositeFormat")] string format,
#if NET10_0_OR_GREATER
            params ReadOnlySpan<object?> args)
#else
            params object[] args)
#endif
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);

            return builder.SetMessage(string.Format(CultureInfo.InvariantCulture, format, args));
        }

        /// <summary>
        /// Adds a location to the error.
        /// </summary>
        /// <param name="node">The syntax node.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder AddLocation(ISyntaxNode node)
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
        /// Adds a location to the error if the error does not already have a location.
        /// </summary>
        /// <param name="node">The syntax node.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder TryAddLocation(ISyntaxNode? node)
        {
            if (node?.Location is null)
            {
                return builder;
            }

            builder.TryAddLocation(new Location(node.Location.Line, node.Location.Column));
            return builder;
        }

        /// <summary>
        /// Adds multiple locations to the error.
        /// </summary>
        /// <param name="nodes">The syntax nodes.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder AddLocations(IEnumerable<ISyntaxNode> nodes)
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
}
