namespace HotChocolate.Data;

public class ExecutableTests(AuthorFixture authorFixture) : IClassFixture<AuthorFixture>
{
    private readonly Author[] _authors = authorFixture.Authors;

    [Fact]
    public void Extensions_Should_ReturnExecutable_When_DBSet()
    {
        // arrange
        // act
        IExecutable<Author> executable = _authors.AsExecutable();

        // assert
        Assert.True(executable is IQueryableExecutable<Author>);
        executable.MatchSnapshot();
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
        Assert.True(executable is IQueryableExecutable<Author>);
        executable.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_ReturnAllItems_When_ToListAsync()
    {
        // arrange
        IExecutable<Author> executable = _authors
            .AsExecutable();

        // act
        object result = await executable.ToListAsync(default);

        // assert
        new { result, executable = executable.Print(), }.MatchSnapshot();
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_SingleOrDefault()
    {
        // arrange
        IExecutable executable = _authors.Take(1).AsExecutable();

        // act
        var result = await executable.SingleOrDefaultAsync(default);

        // assert
        new { result, executable = executable.Print(), }
            .MatchSnapshot(postFix: TestEnvironment.TargetFramework);
    }

    [Fact]
    public async Task ExecuteAsync_Should_OnlyOneItem_When_FirstOrDefault()
    {
        // arrange
        IExecutable executable = _authors.AsExecutable();

        // act
        var result = await executable.FirstOrDefaultAsync(default);

        // assert
        new { result, executable = executable.Print(), }.MatchSnapshot();
    }
}
