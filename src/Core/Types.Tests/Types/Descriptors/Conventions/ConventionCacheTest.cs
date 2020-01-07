using System.Collections.Generic;
using System.Linq;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Types.Descriptors
{
    public class ConventionCacheTest
    {
        [Fact]
        public void GetOrAdd_ShouldResolveConfiguration()
        {
            //arrange
            var serviceProvider = new EmptyServiceProvider();
            var convention = new TestConvention();
            IEnumerable<ConfigureNamedConvention> configurations =
                Enumerable.Empty<ConfigureNamedConvention>()
                    .Append(
                        new ConfigureNamedConvention(
                            Convention.DefaultName,
                            typeof(TestConvention), (s) => convention));
            var conventionCache = ConventionCache.Create(serviceProvider, configurations);

            //act 
            TestConvention first =
                conventionCache.GetOrAdd<TestConvention>(
                    Convention.DefaultName, (s) => new TestConvention());

            //assert
            Assert.Equal(convention, first);
        }

        [Fact]
        public void GetOrAdd_ShouldExtendCache()
        {
            //arrange
            var serviceProvider = new EmptyServiceProvider();
            IEnumerable<ConfigureNamedConvention> configurations =
                Enumerable.Empty<ConfigureNamedConvention>();
            var conventionCache = ConventionCache.Create(serviceProvider, configurations);
            var convention = new TestConvention();

            //act
            TestConvention first =
                conventionCache.GetOrAdd<TestConvention>("test", (s) => convention);
            TestConvention second =
                conventionCache.GetOrAdd<TestConvention>("test", (s) => new TestConvention());

            //assert
            Assert.Equal(convention, first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void GetOrAdd_ShouldCreateConventionWithServiceFactory()
        {
            //arrange
            var serviceProvider = new EmptyServiceProvider();
            IEnumerable<ConfigureNamedConvention> configurations =
                Enumerable.Empty<ConfigureNamedConvention>();
            var conventionCache = ConventionCache.Create(serviceProvider, configurations);

            //act
            TestConvention instance =
                conventionCache.GetOrAdd<TestConvention>("test",
                    (s) => s.GetService<TestConvention>());

            //assert
            Assert.NotNull(instance);
            Assert.IsType<TestConvention>(instance);
        }

        [Fact]
        public void GetOrAdd_ShouldThrowSchemaExceptionIfInstanceIsNull()
        {
            //arrange
            var serviceProvider = new EmptyServiceProvider();
            IEnumerable<ConfigureNamedConvention> configurations =
                Enumerable.Empty<ConfigureNamedConvention>();
            var conventionCache = ConventionCache.Create(serviceProvider, configurations);

            //act /assert
            Assert.Throws<SchemaException>(() =>
                conventionCache.GetOrAdd<TestConvention>("test", (s) => null));
        }

        [Fact]
        public void GetOrAdd_ShouldThrowSchemaExceptionIfInstanceIsOfWrongType()
        {
            //arrange 
            var serviceProvider = new EmptyServiceProvider();
            var convention = new TestConvention2();
            IEnumerable<ConfigureNamedConvention> configurations =
                Enumerable.Empty<ConfigureNamedConvention>()
                    .Append(
                        new ConfigureNamedConvention(
                            Convention.DefaultName,
                            typeof(TestConvention), (s) => convention));
            var conventionCache = ConventionCache.Create(serviceProvider, configurations);
            //act /assert
            Assert.Throws<SchemaException>(() =>
                conventionCache.GetOrAdd<TestConvention>(
                    Convention.DefaultName, (s) => new TestConvention()));
        }

        private class TestConvention : IConvention
        {

        }

        private class TestConvention2 : IConvention
        {

        }

    }
}
