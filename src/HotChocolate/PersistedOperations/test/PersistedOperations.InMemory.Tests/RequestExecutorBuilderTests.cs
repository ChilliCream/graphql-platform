using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.PersistedOperations.InMemory;

public class RequestExecutorBuilderTests
{
    [Fact]
    public void AddFileSystemOperationDocumentStorage_2_Services_Is_Null()
    {
        // arrange
        // act
        void Action()
            => HotChocolateInMemoryPersistedOperationsRequestExecutorBuilderExtensions
                .AddInMemoryOperationDocumentStorage(null!);

        Assert.Throws<ArgumentNullException>(Action);
    }
}
