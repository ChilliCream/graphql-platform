using System.Threading.Tasks;
using Moq;
using Xunit;

namespace StrawberryShake.Abstractions
{
    public class OperationStoreTests
    {
        [Fact]
        public async Task Store_Result()
        {
            // arrange
            var document = new Mock<IDocument>();
            var result = new Mock<IOperationResult<string>>();

            var store = new OperationStore();
            var request = new OperationRequest("abc", document.Object);

            // act
            await store.SetAsync(request, result.Object);

            // assert
            var success =store.TryGet(request, out IOperationResult<string>? retrieved);

            Assert.True(success);
            Assert.Same(result.Object, retrieved);
        }

        publi
    }
}
