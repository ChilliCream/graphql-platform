#if NET9_0_OR_GREATER
using System.Collections;
using System.Data;
using System.Data.Common;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Internal;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

// Verifies that ToStreamPageAsync always releases the lifetime and surfaces the original fault,
// with any disposal failure attached to it instead of replacing it, across every fault path.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingFaultTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_PrimingThrows()
    {
        // arrange: the fault is injected from inside ReadAsync, after EF has already opened the
        // row query's reader.
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 5);
        var exception = new InvalidOperationException("priming boom");
        var readerDisposal = new DataReaderDisposalInterceptor();
        var context = new CatalogContext(
            connectionString,
            [new ReaderFaultingInterceptor(() => exception), readerDisposal]);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: the priming fault surfaces unchanged, the lifetime it owns is released once, and
        // the reader EF opened for the row query is genuinely disposed
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, readerDisposal.DisposedCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_AttachLifetimeDisposalFailure_Not_ReplaceTheFault_When_PrimingThrows_And_LifetimeDisposeAsyncThrows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 5);
        var primingException = new InvalidOperationException("priming boom");
        var lifetimeException = new InvalidOperationException("lifetime boom");
        var context = new CatalogContext(connectionString, [new ThrowingReaderInterceptor(primingException)]);
        var lifetime = new ThrowingLifetime(context, lifetimeException);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: the priming fault surfaces, with the lifetime's disposal failure attached
        Assert.Same(primingException, thrown);
        Assert.Equal([lifetimeException], OrderedDisposal.GetAttached(thrown));
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_CancelledBeforePriming()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 5);
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cts.Token).AsTask());

        // assert: a priming call cancelled before it starts still releases the lifetime once
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_CancelledDuringFirstRowRead()
    {
        // arrange: the cancellation fires from inside the first row's read, after EF has already
        // opened a real reader for it, unlike cancelling the token before priming even starts.
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 5);
        using var cts = new CancellationTokenSource();
        var readerDisposal = new DataReaderDisposalInterceptor();
        var faulting = new ReaderFaultingInterceptor(() =>
        {
            cts.Cancel();
            return new OperationCanceledException(cts.Token);
        });
        var context = new CatalogContext(connectionString, [faulting, readerDisposal]);
        var lifetime = new RecordingLifetime(context);

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cts.Token).AsTask());

        // assert: the cancellation surfaces, the lifetime is released once, and the reader EF had
        // already opened is genuinely disposed rather than left dangling
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, readerDisposal.DisposedCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_EmptyRowEnumeratorDisposeThrows()
    {
        // arrange: no row matches, so the enumerator's own closing dispose is what fails.
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 0);
        var exception = new InvalidOperationException("close boom");
        var context = new CatalogContext(connectionString, [new ThrowingReaderCloseInterceptor(exception)]);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: the reader-close fault surfaces, and the lifetime is still released
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_CountOnlyCountThrows()
    {
        // arrange: IncludeItems is false, so the count is the only query the count-only path
        // runs, and it is the one that fails here.
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 5);
        var exception = new InvalidOperationException("count boom");
        var connectionDisposal = new ConnectionDisposalInterceptor();
        var context = new CatalogContext(
            connectionString,
            [new ThrowingReaderInterceptor(exception), connectionDisposal]);
        var lifetime = new RecordingLifetime(context);
        var arguments = new PagingArguments(2, includeTotalCount: true) { IncludeItems = false };
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                arguments,
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: the count fault surfaces, the lifetime is released once, and EF genuinely
        // disposed the underlying database connection rather than only the lifetime wrapper
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, connectionDisposal.DisposedCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_EmptyRowCountThrows()
    {
        // arrange: no row matches, so priming succeeds empty and its own reader is disposed
        // normally; the separate count query issued afterward is what fails.
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 0);
        var exception = new InvalidOperationException("count boom");
        var readerDisposal = new DataReaderDisposalInterceptor();
        var context = new CatalogContext(
            connectionString,
            [new ThrowingSecondReaderInterceptor(exception), readerDisposal]);
        var lifetime = new RecordingLifetime(context);
        var arguments = new PagingArguments(2, includeTotalCount: true);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                arguments,
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: the count fault surfaces, the lifetime is released once, and the empty row
        // query's own reader was genuinely disposed rather than only the lifetime wrapper
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, readerDisposal.DisposedCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_FirstAndLastAreBothSpecified()
    {
        // arrange: validation runs before any query, so the lifetime must still be released.
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(first: 2, last: 2),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: specifying both `first` and `last` still releases the lifetime once
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_SourceHasNoOrderBy()
    {
        // arrange: an unordered source fails cursor-key validation before any query runs.
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands.ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: an unordered source still releases the lifetime once
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_AfterIsAnEndCursorWithFirst()
    {
        // arrange: an end cursor is only meaningful with `before` and `last`; combining it with
        // `first` trips validation before any query runs.
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var endCursor = CursorFormatter.FormatEndCursor(0, 5);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2, after: endCursor),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: an end cursor combined with `first` still releases the lifetime once
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_AfterCursorIsMalformed()
    {
        // arrange: two cursor values encoded for a source ordered by only one key trips the
        // "number of keys must match the number of values" guard while parsing the cursor.
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var malformedCursor = Convert.ToBase64String("a:b"u8);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands.OrderBy(t => t.Name).ToStreamPageAsync(
                new PagingArguments(2, after: malformedCursor),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: a malformed cursor still releases the lifetime once
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_When_IncludeItemsIsFalseWithoutTotalCount()
    {
        // arrange: IncludeItems is false and the total count was not requested either, so there is
        // no query left for the count-only path to run, which trips validation before any query runs.
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2) { IncludeItems = false },
                includeTotalCount: false,
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: IncludeItems false without a total count still releases the lifetime once
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_GroupedCountThrows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 3);
        var exception = new InvalidOperationException("batch count boom");
        var connectionDisposal = new ConnectionDisposalInterceptor();
        var context = new CatalogContext(
            connectionString,
            [new ThrowingReaderInterceptor(exception), connectionDisposal]);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands
                .Where(t => new[] { "Item0001", "Item0002", "Item0003" }.Contains(t.Name))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.Name,
                    new PagingArguments(2),
                    includeTotalCount: true,
                    lifetime: lifetime,
                    cancellationToken: cancellationToken).AsTask());

        // assert
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, connectionDisposal.DisposedCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_DistinctKeysQueryThrows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 3);
        var exception = new InvalidOperationException("distinct keys boom");
        var connectionDisposal = new ConnectionDisposalInterceptor();
        var context = new CatalogContext(
            connectionString,
            [new ThrowingReaderInterceptor(exception), connectionDisposal]);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands
                .Where(t => t.Name != null)
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.Name,
                    new PagingArguments(2),
                    lifetime: lifetime,
                    cancellationToken: cancellationToken)
                .AsTask());

        // assert
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, connectionDisposal.DisposedCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_KeyEvaluationThrows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands
                .Where(t => GetThrowingKeys().Contains(t.Name))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.Name,
                    new PagingArguments(2),
                    lifetime: lifetime,
                    cancellationToken: cancellationToken)
                .AsTask());

        // assert
        Assert.Equal("key evaluation boom", thrown.Message);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_When_FirstAndLastAreBothSpecified()
    {
        // arrange
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands
                .Where(t => new[] { "Item0001" }.Contains(t.Name))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.Name,
                    new PagingArguments(first: 2, last: 2),
                    lifetime: lifetime,
                    cancellationToken: cancellationToken).AsTask());

        // assert
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_When_IncludeItemsIsFalseWithoutTotalCount()
    {
        // arrange
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands
                .Where(t => new[] { "Item0001" }.Contains(t.Name))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.Name,
                    new PagingArguments(2) { IncludeItems = false },
                    includeTotalCount: false,
                    lifetime: lifetime,
                    cancellationToken: cancellationToken).AsTask());

        // assert
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_When_NoRequestedKeyHasAnyRows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        var context = new CatalogContext(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        await context.Database.EnsureCreatedAsync(cancellationToken);
        var lifetime = new RecordingLifetime(context);

        // act
        var pages = await context.Brands
            .Where(t => new[] { "Item0001", "Item0002" }.Contains(t.Name))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(
                t => t.Name,
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cancellationToken);

        foreach (var page in pages.Values)
        {
            await page.DisposeAsync();
        }

        // assert
        Assert.Equal(2, pages.Count);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_When_ContainsKeySetContainsNull()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 1);
        var context = new CatalogContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        using var capture = new CapturePagingQueryInterceptor();

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands
                .Where(t => new string?[] { "Item0001", null }.Contains(t.DisplayName))
                .OrderBy(t => t.DisplayName)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.DisplayName!,
                    new PagingArguments(2),
                    lifetime: lifetime,
                    cancellationToken: cancellationToken).AsTask());

        // assert: no command executed
        Assert.Equal("keySelector", exception.ParamName);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Empty(capture.Queries);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ReleaseLifetimeOnce_When_DistinctKeysContainNull()
    {
        // arrange
        var connectionString = CreateConnectionString();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using (var seedContext = new CatalogContext(connectionString))
        {
            await seedContext.Database.EnsureCreatedAsync(cancellationToken);

            seedContext.Brands.Add(new Brand
            {
                Name = "Item0001",
                DisplayName = null,
                BrandDetails = new() { Country = new() { Name = "Country0001" } }
            });

            await seedContext.SaveChangesAsync(cancellationToken);
        }

        var connectionDisposal = new ConnectionDisposalInterceptor();
        var context = new CatalogContext(connectionString, [connectionDisposal]);
        var lifetime = new RecordingLifetime(context);
        using var capture = new CapturePagingQueryInterceptor();

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => context.Brands
                .OrderBy(t => t.DisplayName)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.DisplayName!,
                    new PagingArguments(2),
                    lifetime: lifetime,
                    cancellationToken: cancellationToken).AsTask());

        // assert: only the distinct-keys probe ran
        Assert.Equal("keySelector", exception.ParamName);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, connectionDisposal.DisposedCount);
        Assert.Single(capture.Queries);
    }

    private static IEnumerable<string> GetThrowingKeys()
        => throw new InvalidOperationException("key evaluation boom");

    private static async Task SeedBrandsAsync(string connectionString, int count)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= count; i++)
        {
            context.Brands.Add(new Brand
            {
                Name = $"Item{i:D4}",
                BrandDetails = new() { Country = new() { Name = $"Country{i:D4}" } }
            });
        }

        await context.SaveChangesAsync();
    }

    // Fails the row query while EF is still executing the reader.
    private sealed class ThrowingReaderInterceptor(Exception exception) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => throw exception;
    }

    // Fails while EF closes the reader.
    private sealed class ThrowingReaderCloseInterceptor(Exception exception) : DbCommandInterceptor
    {
        public override ValueTask<InterceptionResult> DataReaderClosingAsync(
            DbCommand command,
            DataReaderClosingEventData eventData,
            InterceptionResult result)
            => throw exception;
    }

    // Fails the second reader-backed query onward, so an empty row query can still succeed
    // before the separate count query that follows it fails.
    private sealed class ThrowingSecondReaderInterceptor(Exception exception) : DbCommandInterceptor
    {
        private int _readerExecutions;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => ++_readerExecutions > 1
                ? throw exception
                : base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    // Counts how many readers EF actually disposed, so a test can assert the underlying EF
    // resource was released rather than only the lifetime wrapper handed to ToStreamPageAsync.
    private sealed class DataReaderDisposalInterceptor : DbCommandInterceptor
    {
        public int DisposedCount { get; private set; }

        public override InterceptionResult DataReaderDisposing(
            DbCommand command,
            DataReaderDisposingEventData eventData,
            InterceptionResult result)
        {
            DisposedCount++;
            return result;
        }
    }

    // Wraps every DbDataReader EF Core opens so a fault can be injected from inside ReadAsync,
    // after a real reader already exists.
    private sealed class ReaderFaultingInterceptor(Func<Exception> createFault) : DbCommandInterceptor
    {
        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
            => new(new FaultingDataReader(result, createFault));

        private sealed class FaultingDataReader(DbDataReader inner, Func<Exception> createFault) : DbDataReader
        {
            public override object this[int ordinal] => inner[ordinal];

            public override object this[string name] => inner[name];

            public override int Depth => inner.Depth;

            public override int FieldCount => inner.FieldCount;

            public override bool HasRows => inner.HasRows;

            public override bool IsClosed => inner.IsClosed;

            public override int RecordsAffected => inner.RecordsAffected;

            public override int VisibleFieldCount => inner.VisibleFieldCount;

            public override bool Read() => throw createFault();

            public override Task<bool> ReadAsync(CancellationToken cancellationToken)
                => throw createFault();

            public override bool NextResult() => inner.NextResult();

            public override Task<bool> NextResultAsync(CancellationToken cancellationToken)
                => inner.NextResultAsync(cancellationToken);

            public override bool GetBoolean(int ordinal) => inner.GetBoolean(ordinal);

            public override byte GetByte(int ordinal) => inner.GetByte(ordinal);

            public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
                => inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

            public override char GetChar(int ordinal) => inner.GetChar(ordinal);

            public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
                => inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

            public override string GetDataTypeName(int ordinal) => inner.GetDataTypeName(ordinal);

            public override DateTime GetDateTime(int ordinal) => inner.GetDateTime(ordinal);

            public override decimal GetDecimal(int ordinal) => inner.GetDecimal(ordinal);

            public override double GetDouble(int ordinal) => inner.GetDouble(ordinal);

            public override Type GetFieldType(int ordinal) => inner.GetFieldType(ordinal);

            public override T GetFieldValue<T>(int ordinal) => inner.GetFieldValue<T>(ordinal);

            public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken)
                => inner.GetFieldValueAsync<T>(ordinal, cancellationToken);

            public override float GetFloat(int ordinal) => inner.GetFloat(ordinal);

            public override Guid GetGuid(int ordinal) => inner.GetGuid(ordinal);

            public override short GetInt16(int ordinal) => inner.GetInt16(ordinal);

            public override int GetInt32(int ordinal) => inner.GetInt32(ordinal);

            public override long GetInt64(int ordinal) => inner.GetInt64(ordinal);

            public override string GetName(int ordinal) => inner.GetName(ordinal);

            public override int GetOrdinal(string name) => inner.GetOrdinal(name);

            public override string GetString(int ordinal) => inner.GetString(ordinal);

            public override object GetValue(int ordinal) => inner.GetValue(ordinal);

            public override int GetValues(object[] values) => inner.GetValues(values);

            public override bool IsDBNull(int ordinal) => inner.IsDBNull(ordinal);

            public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
                => inner.IsDBNullAsync(ordinal, cancellationToken);

            public override DataTable? GetSchemaTable() => inner.GetSchemaTable();

            public override IEnumerator GetEnumerator() => ((IEnumerable)inner).GetEnumerator();

            public override void Close() => inner.Close();

            public override Task CloseAsync() => inner.CloseAsync();

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    inner.Dispose();
                }
            }

            public override ValueTask DisposeAsync() => inner.DisposeAsync();
        }
    }

    private sealed class RecordingLifetime(IAsyncDisposable inner) : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            await inner.DisposeAsync();
        }
    }

    // Counts how many times EF actually disposed a database connection.
    private sealed class ConnectionDisposalInterceptor : DbConnectionInterceptor
    {
        public int DisposedCount { get; private set; }

        public override void ConnectionDisposed(DbConnection connection, ConnectionEndEventData eventData)
            => DisposedCount++;

        public override Task ConnectionDisposedAsync(DbConnection connection, ConnectionEndEventData eventData)
        {
            DisposedCount++;
            return Task.CompletedTask;
        }
    }

    // Disposes the inner resource fully before failing.
    private sealed class ThrowingLifetime(IAsyncDisposable inner, Exception exception) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            await inner.DisposeAsync();
            throw exception;
        }
    }
}
#endif
