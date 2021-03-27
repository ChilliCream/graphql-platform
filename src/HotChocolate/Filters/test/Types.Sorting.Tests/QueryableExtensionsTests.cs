using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace HotChocolate.Types.Sorting
{
    [Obsolete]
    public class QueryableExtensionsTests
    {
        [Fact]
        public void CompileInitialSortOperation_AscOnIQueryable_ShouldAddOrderBy()
        {
            // arrange
            IQueryable<Foo> source = new Foo[0].AsQueryable();
            var closure = new SortQueryableClosure(typeof(Foo), "p");
            closure.EnqueueProperty(typeof(Foo).GetProperty(nameof(Foo.Bar)));
            SortOperationInvocation operation =
                closure.CreateSortOperation(SortOperationKind.Asc);

            // act
            Expression sortExpression = source.Expression.CompileInitialSortOperation(
                operation
            );
            IQueryable<Foo> sorted =
                source.Provider.CreateQuery<Foo>(sortExpression);

            // assert
            Assert.Equal(source.OrderBy(s => s.Bar), sorted);
        }

        [Fact]
        public void CompileInitialSortOperation_DescOnIQueryable_ShouldAddOrderBy()
        {
            // arrange
            IQueryable<Foo> source = new Foo[0].AsQueryable();
            var closure = new SortQueryableClosure(typeof(Foo), "p");
            closure.EnqueueProperty(typeof(Foo).GetProperty(nameof(Foo.Bar)));
            SortOperationInvocation operation =
                closure.CreateSortOperation(SortOperationKind.Desc);

            // act
            Expression sortExpression = source.Expression.CompileInitialSortOperation(
                operation
            );

            IQueryable<Foo> sorted =
               source.Provider.CreateQuery<Foo>(sortExpression);


            // assert
            Assert.Equal(source.OrderByDescending(s => s.Bar), sorted);
        }

        [Fact]
        public void AddSortOperation_AscOnIOrderedQueryable_ShouldAddThenBy()
        {
            // arrange
            IOrderedQueryable<Foo> source = new Foo[0].AsQueryable().OrderBy(f => f.Bar);
            var closure = new SortQueryableClosure(typeof(Foo), "p");
            closure.EnqueueProperty(typeof(Foo).GetProperty(nameof(Foo.Bar)));
            SortOperationInvocation operation =
                closure.CreateSortOperation(SortOperationKind.Asc);

            // act
            Expression sortExpression = source.Expression.CompileInitialSortOperation(
                operation
            );
            IQueryable<Foo> sorted =
               source.Provider.CreateQuery<Foo>(sortExpression);


            // assert
            Assert.Equal(source.ThenBy(s => s.Bar), sorted);
        }

        [Fact]
        public void AddSortOperation_DescOnIOrderedQueryable_ShouldAddThenByDescending()
        {
            // arrange
            IOrderedQueryable<Foo> source = new Foo[0].AsQueryable().OrderBy(f => f.Bar);
            var closure = new SortQueryableClosure(typeof(Foo), "p");
            closure.EnqueueProperty(typeof(Foo).GetProperty(nameof(Foo.Bar)));
            SortOperationInvocation operation =
                closure.CreateSortOperation(SortOperationKind.Desc);

            // act
            Expression sortExpression = source.Expression.CompileInitialSortOperation(
                operation
            );
            IQueryable<Foo> sorted =
               source.Provider.CreateQuery<Foo>(sortExpression);


            // assert
            Assert.Equal(source.ThenByDescending(s => s.Bar), sorted);
        }

        private class Foo
        {
            public string Bar { get; set; }
        }
    }
}
