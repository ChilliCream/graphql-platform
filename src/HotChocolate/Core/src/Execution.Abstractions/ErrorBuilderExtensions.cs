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
        public ErrorBuilder SetInputPath(Path inputPath)
        {
            ArgumentNullException.ThrowIfNull(builder);

            return builder.SetExtension(nameof(inputPath), inputPath);
        }

        /// <summary>
        /// Sets the message of the error.
        /// </summary>
        /// <param name="format">The format of the message.</param>
        /// <param name="args">The arguments for the message.</param>
        /// <returns>The error builder.</returns>
        public ErrorBuilder SetMessage(string format, params object[] args)
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentException.ThrowIfNullOrEmpty(format);
            ArgumentNullException.ThrowIfNull(args);

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
