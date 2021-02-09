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
                HotChocolateInMemoryPersistedQueriesServiceCollectionExtensions
                    .AddInMemoryQueryStorage(null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void AddReadOnlyFileSystemQueryStorage_Services_Is_Null()
        {
            // arrange
            // act
            Action action = () =>
                HotChocolateInMemoryPersistedQueriesServiceCollectionExtensions
                    .AddReadOnlyInMemoryQueryStorage(null!);

            // assert
            Assert.Throws<ArgumentNullException>(action);
        }
    }
}
