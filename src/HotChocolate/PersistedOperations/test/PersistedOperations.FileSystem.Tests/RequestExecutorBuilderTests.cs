using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedOperations.FileSystem;

public class RequestExecutorBuilderTests
{
    [Fact]
    public void AddFileSystemOperationDocumentStorage_2_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateFileSystemPersistedOperationsRequestExecutorBuilderExtensions
                .AddFileSystemOperationDocumentStorage(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }
}
