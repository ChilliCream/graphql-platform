using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace HotChocolate.Execution
{
    public class DeferredQueryResultTests
    {
        [Fact]
        public void RegisterDisposable_Disposable_Is_Null()
        {
            // arrange
            var queryResult = new Mock<IQueryResult>();
            var deferred = new Mock<IAsyncEnumerable<IQueryResult>>();
            var result = new DeferredQueryResult(queryResult.Object, deferred.Object);

            // act
            void Action() => result.RegisterDisposable(null!);

            // assert
            Assert.Throws<ArgumentNullException>(Action);
        }

        [Fact]
        public async Task RegisterDisposable()
        {
            // arrange
            var queryResult = new Mock<IQueryResult>();
            var deferred = new Mock<IAsyncEnumerable<IQueryResult>>();
            var result = new DeferredQueryResult(queryResult.Object, deferred.Object);
            var disposable = new TestDisposable();

            // act
            result.RegisterDisposable(disposable);

            // assert
            await result.DisposeAsync();
            Assert.True(disposable.IsDisposed);
        }

        public class TestDisposable : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }
    }
}
