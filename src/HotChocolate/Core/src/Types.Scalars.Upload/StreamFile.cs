using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Types.Scalars.Upload.Properties;

namespace HotChocolate.Types
{
    /// <summary>
    /// An implementation of <see cref="IFile"/> that allows to pass in streams into the
    /// execution engine.
    /// </summary>
    public class StreamFile : IFile
    {
        private readonly Func<Stream> _openReadStream;

        /// <summary>
        /// Creates a new instance of <see cref="StreamFile"/>.
        /// </summary>
        /// <param name="name">
        /// The file name.
        /// </param>
        /// <param name="openReadStream">
        /// A delegate to open the stream.
        /// </param>
        /// <param name="length">
        /// The file length if available.
        /// </param>
        /// <exception cref="ArgumentException">
        /// <paramref name="name"/> is <c>null</c> or <see cref="String.Empty"/>.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="openReadStream"/> is <c>null</c>.
        /// </exception>
        public StreamFile(
            string name,
            Func<Stream> openReadStream,
            long? length = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(
                    UploadResources.StreamFile_Constructor_NameCannotBeNullOrEmpty,
                    nameof(name));
            }

            Name = name;
            _openReadStream = openReadStream ??
                              throw new ArgumentNullException(nameof(openReadStream));
            Length = length;
        }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public long? Length { get; }

        /// <inheritdoc />
        public virtual async Task CopyToAsync(
            Stream target,
            CancellationToken cancellationToken = default)
        {
#if NETSTANDARD2_0 || NETSTANDARD2_1
            using Stream stream = OpenReadStream();
#else
            await using Stream stream = OpenReadStream();
#endif

#if NETSTANDARD2_0
            await stream.CopyToAsync(target, 1024, cancellationToken).ConfigureAwait(false);
#else
            await stream.CopyToAsync(target, cancellationToken).ConfigureAwait(false);
#endif
        }

        /// <inheritdoc />
        public virtual Stream OpenReadStream() => _openReadStream();
    }
}
