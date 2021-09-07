namespace HotChocolate.Data
{
    using System.Linq;
    using System.Threading.Tasks;
    using Snapshooter.Xunit;
    using Xunit;

    public class NHibernateExecutableTests : IClassFixture<AuthorFixture>
    {
        private readonly IQueryable<Author> _authors;

        public NHibernateExecutableTests(AuthorFixture authorFixture)
        {
            _authors = authorFixture.Authors;
        }


        [Fact]
        public void Extensions_Should_ReturnEntityFrameworkExecutable_When_Queryable()
        {
            // arrange
            // act
            IExecutable<Author> executable = _authors
                .AsQueryable()
                .AsNhibernateExecutable();


            // assert
            Assert.IsType<NHibernateExecutable<Author>>(executable);
            executable.Print().MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
        {
            // arrange
            IExecutable<Author> executable = _authors
                .AsExecutable();

            // act
            object? result = await executable.ToListAsync(default);

            // assert
            new {result, executable = executable.Print()}.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
        {
            // arrange
            IExecutable executable = _authors.Take(1).AsNhibernateExecutable();

            // act
            var result = await executable.SingleOrDefaultAsync(default);

            // assert
            new {result, executable = executable.Print()}.MatchSnapshot();
        }

        [Fact]
        public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
        {
            // arrange
            IExecutable executable = _authors.AsExecutable();

            // act
            var result = await executable.FirstOrDefaultAsync(default);

            // assert
            new {result, executable = executable.Print()}.MatchSnapshot();
        }
    }
}
