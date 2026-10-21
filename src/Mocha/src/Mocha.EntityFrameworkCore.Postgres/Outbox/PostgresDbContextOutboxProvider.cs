using System.Data.Common;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Mocha.EntityFrameworkCore;
using Mocha.Middlewares;
using Mocha.Utils;

namespace Mocha.Outbox;

internal sealed class PostgresDbContextOutboxProvider(
    PostgresOutboxTracker tracker,
    Func<PostgresMessageOutboxOptions> getOptions)
    : IDbContextOutboxProvider, IDisposable
{
    private PooledArrayWriter? _arrayWriter;
    private Utf8JsonWriter? _jsonWriter;
    private DbConnection? _validatedConnection;
    private string? _validatedConnectionString;
    private IModel? _validatedModel;
    private DbContextId _validatedLease;

    public void Validate(DbContext context)
    {
        var connection = context.Database.GetDbConnection();
        var connectionString = connection.ConnectionString;
        if (ReferenceEquals(_validatedConnection, connection)
            && _validatedConnectionString == connectionString
            && ReferenceEquals(_validatedModel, context.Model)
            && _validatedLease == context.ContextId)
        {
            return;
        }

        if (!getOptions().MatchesDestination(context))
        {
            throw Mocha.EntityFrameworkCore.Postgres.ThrowHelper.OutboxDestinationNotRegistered(context.GetType());
        }

        _validatedConnection = connection;
        _validatedConnectionString = connectionString;
        _validatedModel = context.Model;
        _validatedLease = context.ContextId;
    }

    public IPreparedOutboxMessage Prepare(MessageEnvelope envelope)
    {
        _arrayWriter ??= new PooledArrayWriter();
        _jsonWriter ??= new Utf8JsonWriter(_arrayWriter);

        try
        {
            var writer = new MessageEnvelopeWriter(_jsonWriter);
            writer.WriteMessage(envelope);
            _jsonWriter.Flush();

#if NET9_0_OR_GREATER
            var id = Guid.CreateVersion7();
#else
            var id = Guid.NewGuid();
#endif
            return new OutboxMessage(id, Encoding.UTF8.GetString(_arrayWriter.WrittenMemory.Span));
        }
        finally
        {
            _jsonWriter.Reset();
            _arrayWriter.Reset();
        }
    }

    public void Stage(DbContext context, IPreparedOutboxMessage message)
        => tracker.Track(context, (OutboxMessage)message);

    public void Dispose()
    {
        _jsonWriter?.Dispose();
        _arrayWriter?.Dispose();
    }
}
