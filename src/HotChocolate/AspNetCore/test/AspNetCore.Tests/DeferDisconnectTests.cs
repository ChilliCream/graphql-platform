using System.Text;
#if !NET11_0_OR_GREATER
using System.Net.Http.Json;
#endif
using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore;

public class DeferDisconnectTests(TestServerFactory serverFactory) : ServerTestBase(serverFactory)
{
    [Theory]
    [InlineData("multipart/mixed")]
    [InlineData("text/event-stream")]
    public async Task Post_Should_ReturnNoErrors_When_ClientDisconnectedDuringDeferredResolver(
        string acceptHeader)
    {
        // arrange
        var gates = new DisconnectGates();
        using var server = CreateServer(gates);
        var client = server.CreateClient();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var disconnect = new CancellationTokenSource();

        using var request = new HttpRequestMessage(HttpMethod.Post, "/graphql");
        request.Content = JsonContent.Create(new { query = "{ fast ... @defer { slow } }" });
        request.Headers.Add("Accept", acceptHeader);

        // act
        var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            disconnect.Token);

        var body = await response.Content.ReadAsStreamAsync(timeout.Token);
        await ReadUntilAsync(body, "\"hasNext\":true", timeout.Token);
        await gates.SlowEntered.Task.WaitAsync(timeout.Token);

        await disconnect.CancelAsync();
        await body.DisposeAsync();
        response.Dispose();
        await Task.Delay(TimeSpan.FromMilliseconds(300), timeout.Token);

        gates.SlowGate.SetResult();
        await gates.SlowReturned.Task.WaitAsync(timeout.Token);
        await Task.Delay(TimeSpan.FromMilliseconds(300), timeout.Token);

        using var followUp = await client.PostAsync(
            "/graphql",
            JsonContent.Create(new { query = "{ fast }" }),
            timeout.Token);
        var followUpBody = await followUp.Content.ReadAsStringAsync(timeout.Token);

        // assert
        followUpBody.MatchInlineSnapshot(
            """
            {"data":{"fast":"fast-value"}}
            """);
    }

    private static async Task ReadUntilAsync(
        Stream stream,
        string expected,
        CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        var text = new StringBuilder();

        while (!text.ToString().Contains(expected, StringComparison.Ordinal))
        {
            var read = await stream.ReadAsync(buffer, cancellationToken);

            if (read == 0)
            {
                throw new InvalidOperationException("The response ended before the initial payload arrived.");
            }

            text.Append(Encoding.UTF8.GetString(buffer, 0, read));
        }
    }

    private TestServer CreateServer(DisconnectGates gates)
        => ServerFactory.Create(
            services => services
                .AddSingleton(gates)
                .AddRouting()
                .AddGraphQLServer()
                .AddQueryType<DisconnectQuery>()
                .AddDefaultBatchDispatcher()
                .ModifyOptions(o => o.EnableDefer = true),
            app => app
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapGraphQL()));

    public sealed class DisconnectGates
    {
        public TaskCompletionSource SlowEntered { get; } = Create();

        public TaskCompletionSource SlowGate { get; } = Create();

        public TaskCompletionSource SlowReturned { get; } = Create();

        private static TaskCompletionSource Create()
            => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public sealed class DisconnectQuery
    {
        public string GetFast() => "fast-value";

        public async Task<string> GetSlowAsync([Service] DisconnectGates gates)
        {
            gates.SlowEntered.TrySetResult();
            await gates.SlowGate.Task;
            gates.SlowReturned.TrySetResult();
            return null!;
        }
    }
}
