using System;
using System.Collections.Generic;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public class QueryableSortVisitorContext
        : SortVisitorContextBase
    {
        private const string _parameterName = "t";

        public QueryableSortVisitorContext(
            InputObjectType initialType,
            Type source,
            bool inMemory)
            : base(initialType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            Closure = new SortQueryableClosure(source, _parameterName);
            InMemory = inMemory;
        }

        public SortQueryableClosure Closure { get; }

        public Queue<SortOperationInvocation> SortOperations { get; } =
            new Queue<SortOperationInvocation>();

        public bool InMemory { get; }
    }
}
