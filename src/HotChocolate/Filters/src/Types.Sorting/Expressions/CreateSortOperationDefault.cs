using System;
using System.Linq;
using System.Linq.Expressions;
using HotChocolate.Types.Sorting.Conventions;

namespace HotChocolate.Types.Sorting.Expressions
{
    public static class CreateSortOperationDefault
    {
        public static SortOperationInvocation CreateSortOperation(
            SortingExpressionVisitorDefinition visitorDefinition,
            QueryableSortVisitorContext context,
            SortOperationKind kind)
        {
            if (context.InMemory)
            {
                return context.Closure.CreateInMemorySortOperation(kind);
            }
            return context.Closure.CreateSortOperation(kind);
        }
    }
}