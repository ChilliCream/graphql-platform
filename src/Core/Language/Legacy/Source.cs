using System;
using System.IO;
using System.Text;

namespace HotChocolate.Language
{
    /// <summary>
    /// Represents a GraphQL source.
    /// </summary>
    [Obsolete("Use the Utf8GraphQLParser.")]
    public sealed class Source
        : ISource
        , IEquatable<Source>
    {
        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="T:HotChocolate.Language.Source"/> class.
        /// </summary>
        /// <param name="text">
        /// The GraphQL source text.
        /// </param>
        public Source(string text)
        {
            Text = text ?? string.Empty;
            Text = Text.Replace("\r\n", "\n")
                .Replace("\n\r", "\n");
        }

        /// <summary>
        /// Gets the GraphQL source text.
        /// </summary>
        /// <returns>
        /// Returns the GraphQL source text.
        /// </returns>
        public string Text { get; }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal
        /// to the current <see cref="T:HotChocolate.Language.Source"/>.
        /// </summary>
        /// <param name="obj">
        /// The <see cref="object"/> to compare with the current
        /// <see cref="T:HotChocolate.Language.Source"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="object"/> is equal
        /// to the current <see cref="T:HotChocolate.Language.Source"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as Source);
        }

        /// <summary>
        /// Determines whether the specified <see cref="Source"/> is equal
        /// to the current <see cref="T:HotChocolate.Language.Source"/>.
        /// </summary>
        /// <param name="other">
        /// The <see cref="Source"/> to compare with the current
        /// <see cref="T:HotChocolate.Language.Source"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Source"/> is equal
        /// to the current <see cref="T:HotChocolate.Language.Source"/>;
        /// otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(Source? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Text.Equals(other.Text, StringComparison.Ordinal);
        }

        /// <summary>
        /// Serves as a hash function for a
        /// <see cref="T:HotChocolate.Language.Source"/> object.
        /// </summary>
        /// <returns>A hash code for this instance that is suitable
        /// for use in hashing algorithms and data structures such as a
        /// hash table.
        /// </returns>
        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents
        /// the current <see cref="T:HotChocolate.Language.Source"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current
        /// <see cref="T:HotChocolate.Language.Source"/>.
        /// </returns>
        public override string? ToString()
        {
            return Text;
        }

        /// <summary>
        /// Reads a GraphQL source from a file.
        /// </summary>
        /// <param name="filePath">
        /// The file path.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Source"/> consisting of the file content.
        /// </returns>
        public static Source FromFile(string filePath)
        {
            if (filePath == null)
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "Could not find the specified GraphQL source file.",
                    filePath);
            }

            return new Source(File.ReadAllText(filePath));
        }

        /// <summary>
        /// Reads a GraphQL source from a file.
        /// </summary>
        /// <param name="file">
        /// The GraphQL source file.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Source"/> consisting of the file content.
        /// </returns>
        public static Source FromFile(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            file.Refresh();
            if (!file.Exists)
            {
                throw new FileNotFoundException(
                    "Could not find the specified GraphQL source file.",
                    file.FullName);
            }

            return new Source(File.ReadAllText(file.FullName));
        }

        /// <summary>
        /// Reads a GraphQL source from a read stream.
        /// </summary>
        /// <param name="stream">
        /// A read stream that provides access to a GraphQL source text.
        /// </param>
        /// <returns>
        /// Returns a <see cref="Source"/> consisting of the streams content.
        /// </returns>
        public static Source FromStream(Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                memoryStream.Position = 0;

                var reader = new StreamReader(stream, Encoding.UTF8);
                return new Source(reader.ReadToEnd());
            }
        }
    }
}
