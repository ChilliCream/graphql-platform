using System;

namespace HotChocolate.Execution
{
    /// <summary>
    /// Represent a executable query.
    /// </summary>
    public interface IQuery
    {
        /// <summary>
        /// Returns the binary query representation.
        /// </summary>
        ReadOnlySpan<byte> ToSource();

        /// <summary>
        /// Returns the query string representation.
        /// </summary>
        string ToString();
    }
}
