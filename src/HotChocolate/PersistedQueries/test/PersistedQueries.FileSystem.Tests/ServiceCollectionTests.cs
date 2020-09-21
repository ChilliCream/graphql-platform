using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.PersistedQueries.FileSystem
{
    public class ServiceCollectionTests
    {
        [Fact]
        public void AddFileSystemQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                    .AddFileSystemQueryStorage(null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddFileSystemQueryStorage_1_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                .AddFileSystemQueryStorage(services, "foo");

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .MatchSnapshot();
        }

        [Fact]
        public void AddFileSystemQueryStorage_2_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                .AddFileSystemQueryStorage(services);

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .MatchSnapshot();
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                    .AddReadOnlyFileSystemQueryStorage(null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_1_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                .AddReadOnlyFileSystemQueryStorage(services, "foo");

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .OrderBy(t => t.Key)
                .MatchSnapshot();
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_2_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                .AddReadOnlyFileSystemQueryStorage(services);

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .OrderBy(t => t.Key)
                .MatchSnapshot();
        }
    }
}
