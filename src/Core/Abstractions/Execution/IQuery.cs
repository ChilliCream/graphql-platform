using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represent a executable query.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Writes the current query to the output stream.
        /// </summary>
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
        ReadOnlySpan<byte> ToSpan();

        /// <summary>
        /// Returns the query string representation.
        /// </summary>
        string ToString();
    }
}
