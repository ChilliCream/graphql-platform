#if NET9_0_OR_GREATER
using System.Data.Common;
using CookieCrumble.Resources;
using GreenDonut.Data.Internal;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingFaultTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ToStreamPageAsync_Should_ReleaseLifetimeOnce_And_SurfaceOriginalFault_When_PrimingThrows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 5);
        var exception = new InvalidOperationException("priming boom");
        var context = new CatalogContext(connectionString, [new ThrowingReaderInterceptor(exception)]);
        var lifetime = new RecordingLifetime(context);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                new PagingArguments(2),
                lifetime: lifetime,
                cancellationToken: cancellationToken).AsTask());

        // assert: the priming fault surfaces unchanged, and the lifetime it owns is released once
        Assert.Same(exception, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
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

    private sealed class RecordingLifetime(IAsyncDisposable inner) : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            await inner.DisposeAsync();
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
