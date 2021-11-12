using System;
using System.Linq;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.PersistedQueries.FileSystem;

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
