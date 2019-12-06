using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;

namespace HotChocolate.Types.Sorting
{
    public class QueryableSortVisitorInMemory
            : QueryableSortVisitor
    {
        private const string _parameterName = "t";

        public QueryableSortVisitorInMemory(
            InputObjectType initialType,
            Type source) : base(initialType, source)
        {

        }

        protected override SortOperationInvocation CreateSortOperation(SortOperationKind kind)
        {
            return Closure.CreateInMemorySortOperation(kind);
        }
    }
}
