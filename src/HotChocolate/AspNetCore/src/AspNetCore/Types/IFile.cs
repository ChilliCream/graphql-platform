using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Types
{
    /// <summary>
    /// Represents a uploaded file.
    /// </summary>
    public interface IFile
    {
        /// <summary>
        /// Gets the file name.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the file length in bytes.
        /// </summary>
        /// <value></value>
        long Length { get; }

        /// <summary>
        /// Asynchronously copies the contents of the uploaded file to the target stream.
        /// </summary>
        /// <param name="target">
        /// The stream to copy the file contents to.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        Task CopyToAsync(
            Stream target, 
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens the request stream for reading the uploaded file.
        /// </summary>
        Stream OpenReadStream();
    }
}
