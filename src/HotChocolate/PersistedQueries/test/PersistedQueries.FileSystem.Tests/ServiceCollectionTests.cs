using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;
using Snapshooter.Xunit;

namespace HotChocolate.PersistedQueries.FileSystem;

public class ServiceCollectionTests
{
    [Fact]
    public void AddFileSystemQueryStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                .AddFileSystemOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddFileSystemQueryStorage_1_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddFileSystemOperationDocumentStorage("foo");

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
        services.AddFileSystemOperationDocumentStorage();

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
        void Action()
            => HotChocolateFileSystemPersistedQueriesServiceCollectionExtensions
                .AddFileSystemOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>((Action) Action);
    }

    [Fact]
    public void AddReadOnlyFileSystemQueryStorage_1_Services()
    {
        // arrange
        var services = new ServiceCollection();

        // act
        services.AddFileSystemOperationDocumentStorage("foo");

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
        services.AddFileSystemOperationDocumentStorage();

        // assert
        services.ToDictionary(
            k => k.ServiceType.GetTypeName(),
            v => v.ImplementationType?.GetTypeName())
            .OrderBy(t => t.Key)
            .MatchSnapshot();
    }
}
