using System.Collections.Generic;
using HotChocolate.Data.Sorting;

namespace HotChocolate.Data.MongoDb.Sorting
{
    public class MongoDbSortVisitorContext : SortVisitorContext<MongoDbSortDefinition>
    {
        public MongoDbSortVisitorContext(ISortInputType initialType)
            : base(initialType)
        {
        }

        /// <summary>
        /// The path from the root to the current position in the input object
        /// </summary>
        public Stack<string> Path { get; } = new Stack<string>();
    }
}
