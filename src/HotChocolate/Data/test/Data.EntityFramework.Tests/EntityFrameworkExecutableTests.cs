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
    public class EntityFrameworkExecutableTests : IClassFixture<AuthorFixture>
    {
        private readonly BookContext _context;

        public EntityFrameworkExecutableTests(AuthorFixture authorFixture)
        {
            _context = authorFixture.Context;
        }


        [Fact]
        public void Extensions_Should_ReturnEntityFrameworkExecutable_When_DBSet()
        {
            // arrange
            // act
            IExecutable<Author> executable = _context.Authors.AsExecutable();

            // assert
            Assert.IsType<EntityFrameworkExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public void Extensions_Should_ReturnEntityFrameworkExecutable_When_Enumerable()
        {
            // arrange
            // act
            IExecutable<Author> executable = _context
                .Authors
                .AsEnumerable()
                .AsEntityFrameworkExecutable();


            // assert
            Assert.IsType<EntityFrameworkExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnAllItems_When_NothingIsApplied()
        {
            // arrange
            IExecutable<Author> executable = _context
                .Authors
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
            IExecutable executable = _context.Authors.Take(1).AsEntityFrameworkExecutable();

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
            IExecutable executable = _context.Authors.AsExecutable();

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
            IExecutable executable = _context.Authors.AsExecutable();
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
            IExecutable executable = _context.Authors.AsExecutable();
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
            IExecutable executable = _context.Authors.AsExecutable();
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
            IExecutable executable = _context.Authors.AsExecutable();
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
