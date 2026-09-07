using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Covers the streamed error model across the error handling modes: an error inside a streamed
/// item, a failing stream source, and an ancestor that is nulled while the stream is still
/// pending.
/// </summary>
public class StreamErrorTests
{
    [Fact]
    public async Task Stream_Should_DeliverNullItemAndContinue_When_ItemErrorsAndItemIsNullableInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ nullableItems @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_EndWithCompletedErrors_When_ItemErrorsAndItemIsNonNullInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_DeliverItemWithNullFieldAndContinue_When_ItemErrorsAndItemIsNullableInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ nullableItems @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_DeliverItemWithNullFieldAndContinue_When_ItemErrorsAndItemIsNonNullInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ items @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_EndWithCompletedErrors_When_SourceYieldsNullForNonNullItemInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ nullYieldingItems @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_DeliverNullItemAndContinue_When_SourceYieldsNullForNonNullItemInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ nullYieldingItems @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_EndWithCompletedErrors_When_SourceFailsAndItemIsNullableInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ nullableFailingSource @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_EndWithCompletedErrors_When_SourceFailsAndItemIsNonNullInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ failingSource @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_EndWithCompletedErrors_When_SourceFailsAndItemIsNullableInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ nullableFailingSource @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_EndWithCompletedErrors_When_SourceFailsAndItemIsNonNullInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ failingSource @stream(initialCount: 1) { index name } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_BeDropped_When_AncestorIsNulledAndItemIsNullableInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ ancestor { nullableItems @stream(initialCount: 1) { index } boom } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_BeDropped_When_AncestorIsNulledAndItemIsNonNullInPropagateMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Propagate);

        // act
        var result = await executor.ExecuteAsync(
            "{ ancestor { items @stream(initialCount: 1) { index } boom } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_Continue_When_AncestorFieldErrorsAndItemIsNullableInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ ancestor { nullableItems @stream(initialCount: 1) { index } boom } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Stream_Should_Continue_When_AncestorFieldErrorsAndItemIsNonNullInNullMode()
    {
        // arrange
        var executor = await CreateExecutorAsync(ErrorHandlingMode.Null);

        // act
        var result = await executor.ExecuteAsync(
            "{ ancestor { items @stream(initialCount: 1) { index } boom } }",
            TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<ResponseStream>(result).MatchMarkdownSnapshot();
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync(ErrorHandlingMode mode)
        => await new ServiceCollection()
            .AddSingleton<StreamGate>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .ModifyRequestOptions(o => o.DefaultErrorHandlingMode = mode)
            .BuildRequestExecutorAsync();

    /// <summary>
    /// Orders the ancestor failure against the stream: the failing field only errors once the
    /// stream task has pulled past its buffered items, and the stream only ends once it has.
    /// </summary>
    public sealed class StreamGate
    {
        private readonly TaskCompletionSource _pulledPastBuffer =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _resumed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task PulledPastBuffer => _pulledPastBuffer.Task;

        public Task Resumed => _resumed.Task;

        public void SignalPulledPastBuffer() => _pulledPastBuffer.TrySetResult();

        public void SignalResumed() => _resumed.TrySetResult();
    }

    public sealed class Query
    {
        public async IAsyncEnumerable<Item> GetItems()
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return new Item(1, fails: true);
            yield return new Item(2, fails: false);
        }

        public async IAsyncEnumerable<Item?> GetNullableItems()
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return new Item(1, fails: true);
            yield return new Item(2, fails: false);
        }

        public async IAsyncEnumerable<Item> GetNullYieldingItems()
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return null!;
            yield return new Item(2, fails: false);
        }

        public async IAsyncEnumerable<Item> GetFailingSource()
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return new Item(1, fails: false);
            throw new GraphQLException("stream source failed");
        }

        public async IAsyncEnumerable<Item?> GetNullableFailingSource()
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return new Item(1, fails: false);
            throw new GraphQLException("stream source failed");
        }

        public Ancestor? GetAncestor() => new();
    }

    public sealed class Ancestor
    {
        public async IAsyncEnumerable<Item> GetItems([Service] StreamGate gate)
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return new Item(1, fails: false);
            gate.SignalPulledPastBuffer();
            await gate.Resumed;
            yield return new Item(2, fails: false);
        }

        public async IAsyncEnumerable<Item?> GetNullableItems([Service] StreamGate gate)
        {
            await Task.Yield();
            yield return new Item(0, fails: false);
            yield return new Item(1, fails: false);
            gate.SignalPulledPastBuffer();
            await gate.Resumed;
            yield return new Item(2, fails: false);
        }

        public async Task<string> GetBoom([Service] StreamGate gate)
        {
            await gate.PulledPastBuffer;
            gate.SignalResumed();
            throw new GraphQLException("ancestor field failed");
        }
    }

    public sealed class Item(int index, bool fails)
    {
        public int Index { get; } = index;

        public string GetName()
        {
            if (fails)
            {
                throw new GraphQLException("item field failed");
            }

            return $"item{Index}";
        }
    }
}
