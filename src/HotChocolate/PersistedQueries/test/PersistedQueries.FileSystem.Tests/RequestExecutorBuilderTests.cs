using System;
using System.Linq;
using HotChocolate.Execution.Configuration;
using HotChocolate.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.PersistedQueries.FileSystem;

public class RequestExecutorBuilderTests
{
    [Fact]
    public void AddFileSystemQueryStorage_2_Services_Is_Null()
    {
        // arrange
        // act
        Action action = () =>
            HotChocolateFileSystemPersistedQueriesRequestExecutorBuilderExtensions
                .AddFileSystemQueryStorage(null!);

        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AddReadOnlyFileSystemQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        Action action = () =>
            HotChocolateFileSystemPersistedQueriesRequestExecutorBuilderExtensions
                .AddReadOnlyFileSystemQueryStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
