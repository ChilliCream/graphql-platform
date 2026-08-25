namespace Mocha.Transport.AzureServiceBus.Tests;

public sealed class DisposableExtensionsTests
{
    [Fact]
    public void DisposeSafe_Should_Dispose_When_DisposableIsPresent()
    {
        // arrange
        var disposable = new TrackingDisposable();

        // act
        disposable.DisposeSafe();

        // assert
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void DisposeSafe_Should_NotThrow_When_DisposableIsNullOrThrows()
    {
        // arrange
        IDisposable? disposable = null;
        var throwing = new TrackingDisposable(throwOnDispose: true);

        // act
        var exception = Record.Exception(() =>
        {
            disposable.DisposeSafe();
            throwing.DisposeSafe();
        });

        // assert
        Assert.Null(exception);
        Assert.True(throwing.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsyncSafe_Should_Dispose_When_DisposableIsPresent()
    {
        // arrange
        var disposable = new TrackingAsyncDisposable();

        // act
        await disposable.DisposeAsyncSafe();

        // assert
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsyncSafe_Should_NotThrow_When_DisposableIsNullOrThrows()
    {
        // arrange
        IAsyncDisposable? disposable = null;
        var throwing = new TrackingAsyncDisposable(throwOnDispose: true);

        // act
        var exception = await Record.ExceptionAsync(async () =>
        {
            await disposable.DisposeAsyncSafe();
            await throwing.DisposeAsyncSafe();
        });

        // assert
        Assert.Null(exception);
        Assert.True(throwing.IsDisposed);
    }

    private sealed class TrackingDisposable(bool throwOnDispose = false) : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
            if (throwOnDispose)
            {
                throw new InvalidOperationException("Dispose failed");
            }
        }
    }

    private sealed class TrackingAsyncDisposable(bool throwOnDispose = false) : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return throwOnDispose
                ? ValueTask.FromException(new InvalidOperationException("Dispose failed"))
                : default;
        }
    }
}
