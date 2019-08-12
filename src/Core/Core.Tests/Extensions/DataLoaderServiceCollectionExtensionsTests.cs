using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using HotChocolate.Integration.DataLoader;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate
{
    public class DataLoaderServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddDataLoader_Services_ServicesIsNull()
        {
            // arrange
            // act
            Action action = () =>
                DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDataLoader_Services()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(services);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoader_Services_MultipleTimes()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(services);
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(services);
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(services);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoader_ServicesFactory_ServicesIsNull()
        {
            // arrange
            // act
            Action action = () =>
                DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(
                    null, s => null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDataLoader_ServicesFactory_FactoryIsNull()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            Action action = () =>
                DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(
                    services, null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDataLoader_ServicesFactory()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(
                services, s => null);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoader_ServicesFactory_MultipleTimes()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(
                services, s => null);
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(
                services, s => null);
            DataLoaderServiceCollectionExtensions.AddDataLoader<TestDataLoader>(
                services, s => null);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoader_ServicesFactory_ServiceToImplementation_ServicesIsNull()
        {
            // arrange
            // act
            Action action = () =>
                DataLoaderServiceCollectionExtensions
                    .AddDataLoader<ITestDataLoader, TestDataLoader>(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDataLoader_ServicesFactory_ServiceToImplementation()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions
                .AddDataLoader<ITestDataLoader, TestDataLoader>(services);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoader_ServicesFactory_ServiceToImplementation_MultipleTimes()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions
                .AddDataLoader<ITestDataLoader, TestDataLoader>(services);
            DataLoaderServiceCollectionExtensions
                .AddDataLoader<ITestDataLoader, TestDataLoader>(services);
            DataLoaderServiceCollectionExtensions
                .AddDataLoader<ITestDataLoader, TestDataLoader>(services);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoaderRegistry_ServicesIsNull()
        {
            // arrange
            // act
            Action action = () =>
                DataLoaderServiceCollectionExtensions
                    .AddDataLoaderRegistry(null);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddDataLoaderRegistry()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions.AddDataLoaderRegistry(services);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }

        [Fact]
        public void AddDataLoaderRegistry_MultipleTimes()
        {
            // arrange
            var services = new ServiceCollection();

            // act
            DataLoaderServiceCollectionExtensions.AddDataLoaderRegistry(services);
            DataLoaderServiceCollectionExtensions.AddDataLoaderRegistry(services);
            DataLoaderServiceCollectionExtensions.AddDataLoaderRegistry(services);

            // assert
            services
                .Select(t => t.ServiceType.GetTypeName())
                .OrderBy(t => t)
                .ToArray()
                .MatchSnapshot();
        }
    }
}
