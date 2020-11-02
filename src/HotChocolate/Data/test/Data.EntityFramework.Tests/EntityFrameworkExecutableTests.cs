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
        public void Extensions_Should_ReturnEntityFrameworkExecutable_When_Queryable()
        {
            // arrange
            // act
            IExecutable<Author> executable = _context
                .Authors
                .AsQueryable()
                .AsEntityFrameworkExecutable();


            // assert
            Assert.IsType<EntityFrameworkExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
        {
            // arrange
            IExecutable<Author> executable = _context
                .Authors
                .AsExecutable();

            // act
            object? result = await executable.ToListAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
        {
            // arrange
            IExecutable executable = _context.Authors.Take(1).AsEntityFrameworkExecutable();

            // act
            object? result = await executable.SingleOrDefaultAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
        {
            // arrange
            IExecutable executable = _context.Authors.AsExecutable();

            // act
            object? result = await executable.FirstOrDefaultAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }
    }
}
