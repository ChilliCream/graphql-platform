using System.Linq;
using System.Threading.Tasks;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data
{
    public class ExecutableTests : IClassFixture<AuthorFixture>
    {
        private readonly Author[] _context;

        public ExecutableTests(AuthorFixture authorFixture)
        {
            _context = authorFixture.Authors;
        }


        [Fact]
        public void Extensions_Should_ReturnExecutable_When_DBSet()
        {
            // arrange
            // act
            IExecutable<Author> executable = _context.AsExecutable();

            // assert
            Assert.IsType<QueryableExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public void Extensions_Should_ReturnExecutable_When_Queryable()
        {
            // arrange
            // act
            IExecutable<Author> executable = _context
                .AsQueryable()
                .AsExecutable();


            // assert
            Assert.IsType<QueryableExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
        {
            // arrange
            IExecutable<Author> executable = _context
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
            IExecutable executable = _context.Take(1).AsExecutable();

            // act
            object? result = await executable.SingleOrDefaultAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
        {
            // arrange
            IExecutable executable = _context.AsExecutable();

            // act
            object? result = await executable.FirstOrDefaultAsync(default);

            // assert
            new { result, executable = executable.Print() }.MatchSnapshot();
        }
    }
}
