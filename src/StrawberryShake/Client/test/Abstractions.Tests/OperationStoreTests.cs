using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace StrawberryShake
{
    public class OperationStoreTests
    {
        [Fact]
        public async Task Store_And_Retrieve_Result()
        {
            // arrange
            var document = new Mock<IDocument>();
            var result = new Mock<IOperationResult<string>>();
            var store = new OperationStore();
            var request = new OperationRequest("abc", document.Object);

            // act
            await store.SetAsync(request, result.Object);
            var success = store.TryGet(request, out IOperationResult<string>? retrieved);


            // assert
            Assert.True(success);
            Assert.Same(result.Object, retrieved);
        }

        [Fact]
        public void TryGet_Not_Found()
        {
            // arrange
            var document = new Mock<IDocument>();
            var store = new OperationStore();
            var request = new OperationRequest("abc", document.Object);

            // act
            var success = store.TryGet(request, out IOperationResult<string>? retrieved);

            // assert
            Assert.False(success);
            Assert.Null(retrieved);
        }

        [Fact]
        public async Task Watch_For_Updates()
        {
            // arrange
            var document = new Mock<IDocument>();
            var result = new Mock<IOperationResult<string>>();
            var store = new OperationStore();
            var request = new OperationRequest("abc", document.Object);
            var observer = new TestObserver();

            // act
            IAsyncDisposable session = await store
                .Watch<string>(request)
                .SubscribeAsync(observer);

            // assert
            await store.SetAsync(request, result.Object);
            Assert.Same(result.Object, observer.LastResult);
            await session.DisposeAsync();
        }

        [Fact]
        public async Task Watch_Unsubscribe()
        {
            // arrange
            var document = new Mock<IDocument>();
            var result = new Mock<IOperationResult<string>>();
            var store = new OperationStore();
            var request = new OperationRequest("abc", document.Object);
            var observer = new TestObserver();

            IAsyncDisposable session = await store
                .Watch<string>(request)
                .SubscribeAsync(observer);

            // act
            await session.DisposeAsync();

            // assert
            await store.SetAsync(request, result.Object);
            Assert.Null(observer.LastResult);
        }

        public class TestObserver : IAsyncObserver<IOperationResult<string>>
        {
            public IOperationResult<string>? LastResult { get; private set; }

            public ValueTask OnCompletedAsync() =>
                default;

            public ValueTask OnErrorAsync(
                Exception error,
                CancellationToken cancellationToken = default) =>
                default;

            public ValueTask OnNextAsync(
                IOperationResult<string> value,
                CancellationToken cancellationToken = default)
            {
                LastResult = value;
                return default;
            }
        }
    }
}
