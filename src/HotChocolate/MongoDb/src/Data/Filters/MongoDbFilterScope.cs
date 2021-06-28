using System.Collections.Generic;
using HotChocolate.Data.Filters;

namespace HotChocolate.Data.MongoDb.Filters
{
    /// <inheritdoc />
    public class MongoDbFilterScope
        : FilterScope<MongoDbFilterDefinition>
    {
        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new Stack<string>();
    }
}
