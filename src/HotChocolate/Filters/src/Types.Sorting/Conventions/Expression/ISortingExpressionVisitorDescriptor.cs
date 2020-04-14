using System.Linq.Expressions;

namespace HotChocolate.Types.Sorting.Conventions
{
    public delegate SortOperationInvocation SortOperationFactory(
        SortingExpressionVisitorDefinition visitorDefinition,
        QueryableSortVisitorContext context,
        SortOperationKind kind);

    public delegate Expression SortCompiler(
        SortingExpressionVisitorDefinition visitorDefinition,
        QueryableSortVisitorContext context,
        Expression source);

    public interface ISortingExpressionVisitorDescriptor
        : ISortingVisitorDescriptor
    {
        ISortingConventionDescriptor And();

        ISortingExpressionVisitorDescriptor Compile(SortCompiler compiler);

        ISortingExpressionVisitorDescriptor CreateOperation(SortOperationFactory facotry);
    }
}
