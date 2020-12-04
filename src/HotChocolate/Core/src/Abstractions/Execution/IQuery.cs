using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represents an executable query.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Writes the current query to the output stream.
        /// </summary>
        [Obsolete("Use WriteToAsync")]
        void WriteTo(Stream output);

        /// <summary>
        /// Writes the current query to the output stream.
        /// </summary>
        Task WriteToAsync(Stream output);

        /// <summary>
        /// Writes the current query to the output stream.
        /// </summary>
        Task WriteToAsync(Stream output, CancellationToken cancellationToken);

        /// <summary>
        /// Returns the binary query representation.
        /// </summary>
        ReadOnlySpan<byte> AsSpan();

        /// <summary>
        /// Returns the query string representation.
        /// </summary>
        string ToString();
    }
}
