using GreenDonut.Data;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.ApolloFederation;

[QueryType]
public static partial class EmployeeQuery
{
    // Reproduces https://github.com/ChilliCream/graphql-platform/issues/9798:
    // a source-generated connection field returning PageConnection<T> combined
    // with Apollo Federation must not crash schema creation.
    [UseConnection]
    public static PageConnection<Employee> GetEmployeeByPageTest(PagingArguments args)
        => new(Page<Employee>.Empty);
}
