using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using HotChocolate.Data.Filters;
using HotChocolate.Data.Projections;
using HotChocolate.Data.Sorting.Expressions;
using HotChocolate.Types.Pagination;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class ExecutableTests : IClassFixture<AuthorFixture>
    {
        private readonly Author[] _authors;

        public ExecutableTests(AuthorFixture authorFixture)
        {
            _authors = authorFixture.Authors;
        }


        [Fact]
        public void Extensions_Should_ReturnExecutable_When_Enumerable()
        {
            // arrange
            // act
            IExecutable<Author> executable = _authors.AsExecutable();

            // assert
            Assert.IsType<EnumerableExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public void Extensions_Should_ReturnExecutable_When_Queryable()
        {
            // arrange
            // act
            IExecutable<Author> executable = _authors
                .AsQueryable()
                .AsExecutable();


            // assert
            Assert.IsType<EnumerableExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnAllItems_When_NothingIsApplied()
        {
            // arrange
            IExecutable<Author> executable = _authors

                .AsExecutable();

            // act
            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
        {
            // arrange
            IExecutable executable = _authors.Take(1).AsExecutable();

            // act
            executable = ((ISingleOrDefaultExecutable)executable).AddSingleOrDefault()!;
            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
        {
            // arrange
            IExecutable executable = _authors.AsExecutable();

            // act
            executable = ((IFirstOrDefaultExecutable)executable).AddFirstOrDefault()!;
            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_Filter()
        {
            // arrange
            IExecutable executable = _authors.AsExecutable();
            Expression<Func<Author, bool>> filter = x => x.Name == "Foo";

            // act
            executable = ((IQueryableFilteringExecutable<Author>)executable).AddFiltering(filter);
            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_Sorting()
        {
            // arrange
            IExecutable executable = _authors.AsExecutable();
            Func<IQueryable<Author>, IQueryable<Author>> sort = x => x.OrderBy(x => x.Name);

            // act
            executable = ((IQueryableSortingExecutable<Author>)executable).AddSorting(sort);
            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_OffsetPaging()
        {
            // arrange
            IExecutable executable = _authors.AsExecutable();
            var pagingOptions = new PagingOptions();
            var pagingArguments = new OffsetPagingArguments(1, 1);

            // act
            executable = ((IOffsetPagingExecutable)executable).AddPaging(
                pagingOptions,
                pagingArguments,
                true);
            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_CursorPaging()
        {
            // arrange
            IExecutable executable = _authors.AsExecutable();
            var pagingOptions = new PagingOptions();
            var pagingArguments = new CursorPagingArguments(1);

            // act
            executable = ((ICursorPagingExecutable)executable).AddPaging(
                pagingOptions,
                pagingArguments);

            object? result = await executable.ExecuteAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }
    }
}
