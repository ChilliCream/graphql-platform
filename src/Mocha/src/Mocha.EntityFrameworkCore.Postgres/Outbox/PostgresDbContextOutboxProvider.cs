using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Mocha.EntityFrameworkCore;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Outbox;

internal sealed class PostgresDbContextOutboxProvider(
    PostgresOutboxTracker tracker,
    TimeProvider timeProvider)
    : IDbContextOutboxProvider, IDisposable
{
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;

    public int CreateCheckpoint() => tracker.CreateCheckpoint();

    public void Discard(DbContext context, int checkpoint) => tracker.Discard(context, checkpoint);

    public void Stage(DbContext context, MessageEnvelope envelope, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _arrayWriter ??= new PooledArrayWriter();
        _jsonWriter ??= new Utf8JsonWriter(_arrayWriter);
        JsonDocument? document = null;

        try
        {
            var writer = new MessageEnvelopeWriter(_jsonWriter);
            writer.WriteMessage(envelope);
            _jsonWriter.Flush();
            var reader = new Utf8JsonReader(_arrayWriter.WrittenMemory.Span);
            document = JsonDocument.ParseValue(ref reader);
            cancellationToken.ThrowIfCancellationRequested();

#if NET9_0_OR_GREATER
            var id = Guid.CreateVersion7();
#else
            var id = Guid.NewGuid();
#endif
            tracker.Track(context, new OutboxMessage(id, document, timeProvider.GetUtcNow().UtcDateTime));
            document = null;
        }
        finally
        {
            document?.Dispose();
            _jsonWriter.Reset();
            _arrayWriter.Reset();
        }
    }

    public void Dispose()
    {
        _jsonWriter?.Dispose();
        _arrayWriter?.Dispose();
    }
}
