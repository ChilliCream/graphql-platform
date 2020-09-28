using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.DataLoader;
using Moq;
using Xunit;

namespace HotChocolate.Resolvers
{
    public class DataLoaderResolverContextExtensionsTests
    {
        [Fact]
        public void BatchDataLoader_1_ContextNull_ArgNullException()
        {
            // arrange
            // act
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    null,
                    new FetchBatch<string, string>((keys, ct) => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)),
                    key: "abc");

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void BatchDataLoader_2_ContextNull_ArgNullException()
        {
            // arrange
            // act
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    null,
                    "abc",
                    new FetchBatch<string, string>((keys, ct) => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void BatchDataLoader_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchBatch<string, string>((keys, ct) => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)));

            // assert
            Assert.Throws<ArgumentException>(a);
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
                    key: "123");

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void BatchDataLoader_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchBatch<string, string>));

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
                    key: "abc");

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void GroupDataLoader_2_ContextNull_ArgNullException()
        {
            // arrange
            var lookup = new Mock<ILookup<string, string>>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    null,
                    "abc",
                    new FetchGroup<string, string>((keys, ct) =>
                        Task.FromResult(lookup.Object)));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void GroupDataLoader_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();
            var lookup = new Mock<ILookup<string, string>>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchGroup<string, string>((keys, ct) =>
                            Task.FromResult(lookup.Object)));

            // assert
            Assert.Throws<ArgumentException>(a);
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
                    key: "123");

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void GroupDataLoader_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchGroup<string, string>));

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
                    new FetchCacheCt<string, string>((keys, ct) =>
                        Task.FromResult(string.Empty)),
                    key: "abc");

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void CacheDataLoader_2_ContextNull_ArgNullException()
        {
            // arrange
            // act
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    null,
                    "abc",
                    new FetchCacheCt<string, string>((keys, ct) =>
                        Task.FromResult(string.Empty)));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void CacheDataLoader_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchCacheCt<string, string>((keys, ct) =>
                            Task.FromResult(string.Empty)));

            // assert
            Assert.Throws<ArgumentException>(a);
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
                    default(FetchCacheCt<string, string>),
                    key: "123");

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void CacheDataLoader_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchCacheCt<string, string>));

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
        [Obsolete]
        public void FetchOnceAsync_2_ContextNull_ArgNullException()
        {
            // arrange
            // act
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    null,
                    "abc",
                    new Func<CancellationToken, Task<string>>(ct =>
                        Task.FromResult(string.Empty)));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        [Obsolete]
        public void FetchOnceAsync_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    resolverContext.Object,
                    null,
                    new Func<CancellationToken, Task<string>>(ct =>
                        Task.FromResult(string.Empty)));

            // assert
            Assert.Throws<ArgumentException>(a);
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

        [Fact]
        [Obsolete]
        public void FetchOnceAsync_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // act
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    resolverContext.Object,
                    "123",
                    default(Func<CancellationToken, Task<string>>));

            // assert
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
