using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                new FetchBatch<string, string>((keys, ct) => Task
                    .FromResult<IReadOnlyDictionary<string, string>>(
                        null)),
                dataLoaderName: "abc");

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
                default(FetchBatch<string, string>),
                dataLoaderName: "123");

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
                null,
                new FetchGroup<string, string>((keys, ct) =>
                    Task.FromResult(lookup.Object)),
                dataLoaderName: "abc");

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
                default(FetchGroup<string, string>),
                dataLoaderName: "123");

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
                null,
                new FetchCache<string, string>((keys, ct) =>
                    Task.FromResult(string.Empty)),
                key: "abc");

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
                default(FetchCache<string, string>),
                key: "123");

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
                null,
                new Func<CancellationToken, Task<string>>(ct =>
                    Task.FromResult(string.Empty)),
                key: "abc");

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
                default(Func<CancellationToken, Task<string>>),
                key: "123");

        // assert
        Assert.Throws<ArgumentNullException>(a);
    }
}
