using System;
using System.Collections.Generic;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitorContext
        : SortVisitorContextBase
    {
        private const string _parameterName = "t";

        public QueryableSortVisitorContext(
            InputObjectType initialType,
            Type source,
            bool inMemory,
            SortingExpressionVisitorDefinition convention)
            : base(initialType)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Convention = convention ?? throw new ArgumentNullException(nameof(convention));
            Closure = new SortQueryableClosure(source, _parameterName);
            InMemory = inMemory;
        }

        public SortQueryableClosure Closure { get; }

        public SortingExpressionVisitorDefinition Convention { get; }

        public Queue<SortOperationInvocation> SortOperations { get; } =
            new Queue<SortOperationInvocation>();

        public bool InMemory { get; }
    }
}
