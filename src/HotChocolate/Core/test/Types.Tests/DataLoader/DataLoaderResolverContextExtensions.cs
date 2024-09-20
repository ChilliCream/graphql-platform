using HotChocolate.Fetching;
using HotChocolate.Types;
using Moq;

namespace HotChocolate.Resolvers;

public class DataLoaderResolverContextExtensionsTests
{
    [Fact]
    public void BatchDataLoader_1_ContextNull_ArgNullException()
    {
        // arrange
        // act
        Action a = () => DataLoaderResolverContextExtensions
            .BatchDataLoader(
                null!,
                new FetchBatch<string, string>((_, _) => Task
                    .FromResult<IReadOnlyDictionary<string, string>>(
                        null)),
                name: "abc");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void BatchDataLoader_1_FetchNull_ArgNullException()
    {
        // arrange
        var resolverContext = new Mock<IResolverContext>();

        // act
        Action a = () => DataLoaderResolverContextExtensions
            .BatchDataLoader(
                resolverContext.Object,
                default(FetchBatch<string, string>)!,
                name: "123");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void GroupDataLoader_1_ContextNull_ArgNullException()
    {
        // arrange
        var lookup = new Mock<ILookup<string, string>>();

        // act
        Action a = () => DataLoaderResolverContextExtensions
            .GroupDataLoader(
                null!,
                new FetchGroup<string, string>((_, _) =>
                    Task.FromResult(lookup.Object)),
                name: "abc");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void GroupDataLoader_1_FetchNull_ArgNullException()
    {
        // arrange
        var resolverContext = new Mock<IResolverContext>();

        // act
        Action a = () => DataLoaderResolverContextExtensions
            .GroupDataLoader(
                resolverContext.Object,
                default(FetchGroup<string, string>)!,
                name: "123");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void CacheDataLoader_1_ContextNull_ArgNullException()
    {
        // arrange
        // act
        Action a = () => DataLoaderResolverContextExtensions
            .CacheDataLoader(
                null!,
                new FetchCache<string, string>((_, _) =>
                    Task.FromResult(string.Empty)),
                name: "abc");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void CacheDataLoader_1_FetchNull_ArgNullException()
    {
        // arrange
        var resolverContext = new Mock<IResolverContext>();

        // act
        Action a = () => DataLoaderResolverContextExtensions
            .CacheDataLoader(
                resolverContext.Object,
                default(FetchCache<string, string>)!,
                name: "123");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void FetchOnceAsync_1_ContextNull_ArgNullException()
    {
        // arrange
        // assert
        Action a = () => DataLoaderResolverContextExtensions
            .FetchOnceAsync(
                null!,
                _ => Task.FromResult(string.Empty),
                name: "abc");

        // act
        Assert.Throws<ArgumentNullException>(a);
    }

    [Fact]
    public void FetchOnceAsync_1_FetchNull_ArgNullException()
    {
        // arrange
        var resolverContext = new Mock<IResolverContext>();

        // act
        Action a = () => DataLoaderResolverContextExtensions
            .FetchOnceAsync(
                resolverContext.Object,
                default(Func<CancellationToken, Task<string>>)!,
                name: "123");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }
}
