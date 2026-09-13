using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NATS.Client.JetStream;

namespace Mocha.Transport.Nats.Tests.Fixtures;

/// <summary>
/// A clean JetStream server for the duration of one test, with a service name no other test uses.
/// </summary>
/// <remarks>
/// NATS has no equivalent of a RabbitMQ vhost or a Postgres database, so isolation is achieved by
/// removing every stream the server holds. Two things make that necessary rather than tidy: a new
/// durable defaults to <see cref="ConsumerConfigDeliverPolicy.All"/> and so replays whatever an
/// earlier test left behind, and a convention stream binds to any stream that already captures its
/// subjects, so one test's stream silently becomes the next test's.
/// </remarks>
public sealed class JetStreamScope : IAsyncDisposable
{
    private readonly INatsJSContext _jetStream;

    internal JetStreamScope(INatsJSContext jetStream, string streamName)
    {
        _jetStream = jetStream;
        StreamName = streamName;
    }

    /// <summary>
    /// Gets a stream name derived from the test, unique across the suite.
    /// </summary>
    public string StreamName { get; }

    /// <summary>
    /// Derives a stream name from the calling test that is stable across runs and short enough to
    /// stay within the length NATS recommends.
    /// </summary>
    internal static string DeriveStreamName(string testName, string filePath)
    {
        var hash = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(filePath + "." + testName)))[..8];

        return "s-" + hash.ToLowerInvariant();
    }

    internal static async ValueTask PurgeAsync(
        INatsJSContext jetStream,
        CancellationToken cancellationToken)
    {
        var names = new List<string>();

        await foreach (var name in jetStream.ListStreamNamesAsync(cancellationToken: cancellationToken))
        {
            names.Add(name);
        }

        foreach (var name in names)
        {
            // Deleting a stream deletes its consumers with it.
            await jetStream.DeleteStreamAsync(name, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await PurgeAsync(_jetStream, CancellationToken.None);
}

public sealed partial class JetStreamFixture
{
    /// <summary>
    /// Wipes the server and returns a scope naming this test's stream.
    /// </summary>
    /// <param name="testName">The calling test, supplied by the compiler.</param>
    /// <param name="filePath">The calling file, supplied by the compiler.</param>
    /// <returns>A scope that wipes the server again when disposed.</returns>
    public async Task<JetStreamScope> CreateScopeAsync(
        [CallerMemberName] string testName = "",
        [CallerFilePath] string filePath = "")
    {
        var jetStream = JetStream;

        await JetStreamScope.PurgeAsync(jetStream, CancellationToken.None);

        return new JetStreamScope(jetStream, JetStreamScope.DeriveStreamName(testName, filePath));
    }
}
