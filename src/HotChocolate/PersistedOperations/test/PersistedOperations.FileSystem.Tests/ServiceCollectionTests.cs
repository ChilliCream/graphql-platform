using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Utilities;

namespace HotChocolate.PersistedOperations.FileSystem;

public class ServiceCollectionTests
{
    [Fact]
    public void AddFileSystemOperationDocumentStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateFileSystemPersistedOperationsServiceCollectionExtensions
                .AddFileSystemOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>(Action);
    }

    [Fact]
    public void AddFileSystemOperationDocumentStorage_1_Services()
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
    public void AddFileSystemOperationDocumentStorage_2_Services()
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
    public void AddReadOnlyFileSystemOperationDocumentStorage_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateFileSystemPersistedOperationsServiceCollectionExtensions
                .AddFileSystemOperationDocumentStorage(null!);

        // assert
        Assert.Throws<ArgumentNullException>((Action) Action);
    }

    [Fact]
    public void AddReadOnlyFileSystemOperationDocumentStorage_1_Services()
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
    public void AddReadOnlyFileSystemOperationDocumentStorage_2_Services()
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
