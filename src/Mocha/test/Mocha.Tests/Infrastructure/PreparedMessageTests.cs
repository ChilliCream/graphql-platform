using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;

namespace Mocha.Tests.Infrastructure;

public class PreparedMessageTests
{
    [Fact]
    public void Dispose_Should_ReturnContextOnce_When_LeaseIsCopied()
    {
        // arrange
        var pool = new ReusingDispatchContextPool();
        var context = pool.Get();
        context.Envelope = new MessageEnvelope { MessageId = "original" };
        var prepared = new PreparedMessage(context, pool);
        var copy = prepared;

        // act
        prepared.Dispose();
        copy.Dispose();

        // assert
        Assert.Equal(1, pool.ReturnCount);
        Assert.Throws<ObjectDisposedException>(() => copy.Envelope);
        Assert.Null(context.Envelope);
    }

    [Fact]
    public void Dispose_Should_PreserveCurrentLease_When_StaleCopyIsDisposedAfterReuse()
    {
        // arrange
        var pool = new ReusingDispatchContextPool();
        var original = new PreparedMessage(pool.Get(), pool);
        var stale = original;
        original.Dispose();
        var context = pool.Get();
        context.Envelope = new MessageEnvelope { MessageId = "current" };
        using var current = new PreparedMessage(context, pool);

        // act
        stale.Dispose();

        // assert
        Assert.Equal(1, pool.ReturnCount);
        Assert.Throws<ObjectDisposedException>(() => stale.Envelope);
        Assert.Equal("current", current.Envelope.MessageId);
    }

    [Fact]
    public void Dispose_Should_ReturnContextOnce_When_CopiesAreDisposedConcurrently()
    {
        // arrange
        var pool = new ReusingDispatchContextPool();
        var prepared = new PreparedMessage(pool.Get(), pool);

        // act
        Parallel.For(0, 100, _ => prepared.Dispose());

        // assert
        Assert.Equal(1, pool.ReturnCount);
        Assert.Throws<ObjectDisposedException>(() => prepared.Envelope);
    }

    [Fact]
    public void Envelope_Should_Throw_When_LeaseIsDefault()
    {
        // arrange
        var prepared = default(PreparedMessage);

        // act
        prepared.Dispose();

        // assert
        Assert.Throws<ObjectDisposedException>(() => prepared.Envelope);
    }

    private sealed class ReusingDispatchContextPool : ObjectPool<DispatchContext>
    {
        private readonly DispatchContext _context = new();
        private int _returnCount;

        public int ReturnCount => Volatile.Read(ref _returnCount);

        public override DispatchContext Get() => _context;

        public override void Return(DispatchContext obj)
        {
            obj.Reset();
            Interlocked.Increment(ref _returnCount);
        }
    }
}
