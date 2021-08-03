using System.Collections.Generic;
using HotChocolate.Data.Filters;
using SqlKata;

namespace HotChocolate.Data.SqlKata.Filters
{
    /// <inheritdoc />
    public class SqlKataFilterScope
        : FilterScope<Query>
    {
        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new Stack<string>();
    }
}
