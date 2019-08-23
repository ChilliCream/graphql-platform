using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    public class QueryableExtensionsTests
    {
        [Fact]
        public void AddInitialSortOperation_AscOnIQueryable_ShouldAddOrderBy()
        {
            // arrange
            IQueryable<Foo> source = new Foo[0].AsQueryable();
            var operation = new SortOperationInvocation(
                SortOperationKind.Asc,
                typeof(Foo).GetProperty(nameof(Foo.Bar)));
            ParameterExpression parameter = Expression.Parameter(typeof(Foo));

            // act
            IOrderedQueryable<Foo> sorted = source.AddInitialSortOperation(
                operation,
                parameter
            );

            // assert
            Assert.Equal(source.OrderBy(s => s.Bar), sorted);
        }

        [Fact]
        public void AddInitialSortOperation_DescOnIQueryable_ShouldAddOrderBy()
        {
            // arrange
            IQueryable<Foo> source = new Foo[0].AsQueryable();
            var operation = new SortOperationInvocation(
                SortOperationKind.Desc,
                typeof(Foo).GetProperty(nameof(Foo.Bar)));
            ParameterExpression parameter = Expression.Parameter(typeof(Foo));

            // act
            IOrderedQueryable<Foo> sorted = source.AddInitialSortOperation(
                operation,
                parameter
            );

            // assert
            Assert.Equal(source.OrderByDescending(s => s.Bar), sorted);
        }

        [Fact]
        public void AddSortOperation_AscOnIOrderedQueryable_ShouldAddThenBy()
        {
            // arrange
            IOrderedQueryable<Foo> source = new Foo[0].AsQueryable().OrderBy(f => f.Bar);
            var operation = new SortOperationInvocation(
                SortOperationKind.Asc,
                typeof(Foo).GetProperty(nameof(Foo.Bar)));
            ParameterExpression parameter = Expression.Parameter(typeof(Foo));

            // act
            IOrderedQueryable<Foo> sorted = source.AddInitialSortOperation(
                operation,
                parameter
            );

            // assert
            Assert.Equal(source.ThenBy(s => s.Bar), sorted);
        }

        [Fact]
        public void AddSortOperation_DescOnIOrderedQueryable_ShouldAddThenByDescending()
        {
            // arrange
            IOrderedQueryable<Foo> source = new Foo[0].AsQueryable().OrderBy(f => f.Bar);
            var operation = new SortOperationInvocation(
                SortOperationKind.Desc,
                typeof(Foo).GetProperty(nameof(Foo.Bar)));
            ParameterExpression parameter = Expression.Parameter(typeof(Foo));

            // act
            IOrderedQueryable<Foo> sorted = source.AddInitialSortOperation(
                operation,
                parameter
            );

            // assert
            Assert.Equal(source.ThenByDescending(s => s.Bar), sorted);
        }

        private class Foo
        {
            public string Bar { get; set; }
        }
    }
}
