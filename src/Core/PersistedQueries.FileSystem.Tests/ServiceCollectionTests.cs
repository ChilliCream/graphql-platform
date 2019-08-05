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
        public void AddFileSystemQueryStorage_1_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                FileSystemQueryStorageServiceCollectionExtensions
                    .AddFileSystemQueryStorage(null, string.Empty);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddFileSystemQueryStorage_1_Directory_Is_Null()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                FileSystemQueryStorageServiceCollectionExtensions
                    .AddFileSystemQueryStorage(services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddFileSystemQueryStorage_1_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            FileSystemQueryStorageServiceCollectionExtensions
                .AddFileSystemQueryStorage(services, "foo");

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .MatchSnapshot();
        }

        [Fact]
        public void AddFileSystemQueryStorage_2_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                FileSystemQueryStorageServiceCollectionExtensions
                    .AddFileSystemQueryStorage(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddFileSystemQueryStorage_2_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            FileSystemQueryStorageServiceCollectionExtensions
                .AddFileSystemQueryStorage(services);

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .MatchSnapshot();
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_1_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                FileSystemQueryStorageServiceCollectionExtensions
                    .AddReadOnlyFileSystemQueryStorage(null, string.Empty);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_1_Directory_Is_Null()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                FileSystemQueryStorageServiceCollectionExtensions
                    .AddReadOnlyFileSystemQueryStorage(services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_1_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            FileSystemQueryStorageServiceCollectionExtensions
                .AddReadOnlyFileSystemQueryStorage(services, "foo");

            // assert
            services.ToDictionary(
                k => k.ServiceType.GetTypeName(),
                v => v.ImplementationType?.GetTypeName())
                .OrderBy(t => t.Key)
                .MatchSnapshot();
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_2_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                FileSystemQueryStorageServiceCollectionExtensions
                    .AddReadOnlyFileSystemQueryStorage(null);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_2_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            FileSystemQueryStorageServiceCollectionExtensions
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
