using HotChocolate.Types.Sorting.Expressions;

namespace HotChocolate.Types.Sorting.Conventions
{
    public static class SortingExpressionVisitorDescriptorExtensions
    {
        public static ISortingExpressionVisitorDescriptor UseDefault(
            this ISortingExpressionVisitorDescriptor descriptor)
                => descriptor
                    .Compile(SortCompilerDefault.Compile)
                    .CreateOperation(CreateSortOperationDefault.CreateSortOperation);
    }
}
