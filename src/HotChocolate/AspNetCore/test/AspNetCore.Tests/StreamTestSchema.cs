using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using HotChocolate.AspNetCore.Tests.Utilities;
using HotChocolate.Resolvers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

/// <summary>
/// A schema with a single streamable list field that is fed by <see cref="StreamSource"/>,
/// so that a test controls when each item completes and when the list ends.
/// </summary>
public static class StreamTestSchema
{
    /// <summary>
    /// An operation that streams every item of the list field after the initial slice.
    /// </summary>
    public const string StreamOperation = "{ items @stream(initialCount: 1) { name } }";

    /// <summary>
    /// Creates a server that exposes the streamable field over HTTP and over WebSockets.
    /// </summary>
    public static TestServer CreateStreamServer(TestServerFactory serverFactory, StreamSource source)
        => serverFactory.Create(
            services => services
                .AddSingleton(source)
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<StreamQuery>()
                .AddHttpResponseFormatter()
                .ModifyOptions(
                    o =>
                    {
                        o.EnableDefer = true;
                        o.EnableStream = true;
                    }),
            app => app
                .UseWebSockets()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

    public sealed class StreamQuery
    {
        public IAsyncEnumerable<StreamItem> GetItems(IResolverContext context)
            => ReadItemsAsync(context.Services.GetRequiredService<StreamSource>());

        private static async IAsyncEnumerable<StreamItem> ReadItemsAsync(
            StreamSource source,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await foreach (var item in source.ReadAllAsync(cancellationToken))
                {
                    yield return new StreamItem(item);
                }
            }
            finally
            {
                source.TrackEnumeratorCleanup();
            }
        }
    }

    public sealed class StreamItem(string name)
    {
        public async Task<string> GetNameAsync(IResolverContext context)
        {
            var source = context.Services.GetRequiredService<StreamSource>();
            await source.WaitAsync(name).WaitAsync(context.RequestAborted);
            return name;
        }
    }

    /// <summary>
    /// The item source of the streamed list. An item is only resolved once it was released.
    /// </summary>
    public sealed class StreamSource
    {
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource> _gates = new();
        private readonly TaskCompletionSource _enumeratorCleanup =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// Completes once the resolver enumerator ran its cleanup.
        /// </summary>
        public Task EnumeratorCleanup => _enumeratorCleanup.Task;

        public void Write(string name) => _channel.Writer.TryWrite(name);

        public void Complete() => _channel.Writer.TryComplete();

        public void Release(string name) => Gate(name).TrySetResult();

        public Task WaitAsync(string name) => Gate(name).Task;

        public IAsyncEnumerable<string> ReadAllAsync(CancellationToken cancellationToken)
            => _channel.Reader.ReadAllAsync(cancellationToken);

        public void TrackEnumeratorCleanup() => _enumeratorCleanup.TrySetResult();

        private TaskCompletionSource Gate(string name)
            => _gates.GetOrAdd(
                name,
                _ => new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously));
    }
}
