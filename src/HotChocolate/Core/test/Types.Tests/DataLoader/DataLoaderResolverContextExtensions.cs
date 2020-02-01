using System;
using System.Collections.Generic;
using System.Linq;
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
            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    null,
                    "abc",
                    new FetchBatch<string, string>(keys => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void BatchDataLoader_1_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchBatch<string, string>(keys => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void BatchDataLoader_1_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchBatch<string, string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void BatchDataLoader_2_ContextNull_ArgNullException()
        {
            // arrange
            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    null,
                    "abc",
                    new FetchBatchCt<string, string>((keys, ct) => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void BatchDataLoader_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchBatchCt<string, string>((keys, ct) => Task
                        .FromResult<IReadOnlyDictionary<string, string>>(
                            null)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void BatchDataLoader_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .BatchDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchBatchCt<string, string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void GroupDataLoader_1_ContextNull_ArgNullException()
        {
            // arrange
            var lookup = new Mock<ILookup<string, string>>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    null,
                    "abc",
                    new FetchGroup<string, string>(keys =>
                        Task.FromResult(lookup.Object)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void GroupDataLoader_1_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();
            var lookup = new Mock<ILookup<string, string>>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchGroup<string, string>(keys =>
                            Task.FromResult(lookup.Object)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void GroupDataLoader_1_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchGroup<string, string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void GroupDataLoader_2_ContextNull_ArgNullException()
        {
            // arrange
            var lookup = new Mock<ILookup<string, string>>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    null,
                    "abc",
                    new FetchGroupCt<string, string>((keys, ct) =>
                        Task.FromResult(lookup.Object)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void GroupDataLoader_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();
            var lookup = new Mock<ILookup<string, string>>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchGroupCt<string, string>((keys, ct) =>
                            Task.FromResult(lookup.Object)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void GroupDataLoader_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .GroupDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchGroupCt<string, string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CacheDataLoader_1_ContextNull_ArgNullException()
        {
            // arrange
            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    null,
                    "abc",
                    new FetchCache<string, string>(keys =>
                        Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CacheDataLoader_1_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchCache<string, string>(keys =>
                        Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void CacheDataLoader_1_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchCache<string, string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CacheDataLoader_2_ContextNull_ArgNullException()
        {
            // arrange
            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    null,
                    "abc",
                    new FetchCacheCt<string, string>((keys, ct) =>
                        Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void CacheDataLoader_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    resolverContext.Object,
                    null,
                    new FetchCacheCt<string, string>((keys, ct) =>
                            Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void CacheDataLoader_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .CacheDataLoader(
                    resolverContext.Object,
                    "123",
                    default(FetchCacheCt<string, string>));

            // act
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
                    "abc",
                    new FetchOnce<string>(() => Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void FetchOnceAsync_1_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    resolverContext.Object,
                    null,
                    new FetchOnce<string>(() => Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void FetchOnceAsync_1_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    resolverContext.Object,
                    "123",
                    default(FetchOnce<string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void FetchOnceAsync_2_ContextNull_ArgNullException()
        {
            // arrange
            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    null,
                    "abc",
                    new FetchOnceCt<string>(ct =>
                        Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }

        [Fact]
        public void FetchOnceAsync_2_KeyNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    resolverContext.Object,
                    null,
                    new FetchOnceCt<string>(ct =>
                        Task.FromResult(string.Empty)));

            // act
            Assert.Throws<ArgumentException>(a);
        }

        [Fact]
        public void FetchOnceAsync_2_FetchNull_ArgNullException()
        {
            // arrange
            var resolverContext = new Mock<IResolverContext>();

            // assert
            Action a = () => DataLoaderResolverContextExtensions
                .FetchOnceAsync(
                    resolverContext.Object,
                    "123",
                    default(FetchOnceCt<string>));

            // act
            Assert.Throws<ArgumentNullException>(a);
        }
    }
}
