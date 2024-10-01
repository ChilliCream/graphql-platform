namespace HotChocolate.Execution;

public class ResponseStreamTests
{
    [Fact]
    public async Task Register_One_Disposables()
    {
        // arrange
        var result = new ResponseStream(() => default!);
        var disposable = new TestDisposable();

        // act
        result.RegisterForCleanup(disposable);

        // assert
        await result.DisposeAsync();
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void Register_Disposable_Result_Is_null()
    {
        // arrange
        var disposable = new TestDisposable();

        // act
        void Fail() => default(ResponseStream)!.RegisterForCleanup(disposable);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void Register_Disposable_Disposable_Is_Null()
    {
        // arrange
        var result = new ResponseStream(() => default!);

        // act
        void Fail() => result.RegisterForCleanup(default(IDisposable)!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Register_One_Async_Cleanup_Func()
    {
        // arrange
        var result = new ResponseStream(() => default!);
        var disposed = false;

        // act
        result.RegisterForCleanup(
            () =>
            {
                disposed = true;
                return default;
            });

        // assert
        await result.DisposeAsync();
        Assert.True(disposed);
    }

    [Fact]
    public void Register_One_Async_Cleanup_Func_Func_is_Null()
    {
        // arrange
        var result = new ResponseStream(() => default!);

        // act
        void Fail() => result.RegisterForCleanup(default!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Register_One_Cleanup_Func()
    {
        // arrange
        var result = new ResponseStream(() => default!);
        var disposed = false;

        // act
        result.RegisterForCleanup(() => disposed = true);

        // assert
        await result.DisposeAsync();
        Assert.True(disposed);
    }

    [Fact]
    public void Register_One_Cleanup_Func_Func_is_Null()
    {
        // arrange
        var result = new ResponseStream(() => default!);

        // act
        void Fail() => result.RegisterForCleanup(default(Action)!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public async Task Register_Two_Disposables()
    {
        // arrange
        var result = new ResponseStream(() => default!);
        var asyncDisposable = new TestAsyncDisposable();
        var disposable = new TestDisposable();

        // act
        result.RegisterForCleanup(asyncDisposable);
        result.RegisterForCleanup(disposable);

        // assert
        await result.DisposeAsync();
        Assert.True(asyncDisposable.IsDisposed);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void Register_One_Async_Disposable_Disposable_Is_Null()
    {
        // arrange
        var result = new ResponseStream(() => default!);

        // act
        void Fail() => result.RegisterForCleanup(default(IAsyncDisposable)!);

        // assert
        Assert.Throws<ArgumentNullException>(Fail);
    }

    [Fact]
    public void ExpectOperationResult()
    {
        // arrange
        IExecutionResult result = new ResponseStream(() => default!);

        // act
        var responseStream = result.ExpectResponseStream();

        // assert
        Assert.NotNull(responseStream);
    }

    [Fact]
    public void ExpectResponseStream()
    {
        // arrange
        IExecutionResult result = new ResponseStream(() => default!);

        // act
        void Fail() => result.ExpectOperationResult();

        // assert
        Assert.Throws<ArgumentException>(Fail);
    }

    public class TestDisposable : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public class TestAsyncDisposable : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return default;
        }
    }
}
