using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Data.Sorting.Expressions
{
    public class ExecutorBuilder
    {
        private readonly ISortInputType _inputType;

        public ExecutorBuilder(ISortInputType inputType)
        {
            _inputType = inputType;
        }

        public Func<T[], T[]> Build<T>(IValueNode Sort)
        {
            var visitorContext = new QueryableSortContext(_inputType, true);
            var visitor = new SortVisitor<QueryableSortContext, QueryableSortOperation>();

            visitor.Visit(Sort, visitorContext);

            Assert.Empty(visitorContext.Errors);

            return elements => visitorContext.Sort(elements.AsQueryable()).ToArray();
        }
    }
}
