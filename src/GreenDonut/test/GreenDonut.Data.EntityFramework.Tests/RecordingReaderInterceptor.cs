using System.Collections;
using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

/// <summary>
/// Records when rows are read from the database, relative to when a test observes them. Wraps
/// every <see cref="DbDataReader"/> returned by EF Core in a delegating reader that logs one
/// <see cref="ReaderEvent"/> per <see cref="DbDataReader.Read"/>/<see cref="DbDataReader.ReadAsync()"/>
/// call, alongside the text of every executed command.
/// </summary>
public sealed class RecordingReaderInterceptor : DbCommandInterceptor
{
    private readonly List<string> _commandTexts = [];
    private readonly List<ReaderEvent> _events = [];

    /// <summary>
    /// The text of every command executed so far, in execution order.
    /// </summary>
    public IReadOnlyList<string> CommandTexts => _commandTexts;

    /// <summary>
    /// Every read event recorded so far, in the order the reads happened.
    /// </summary>
    public IReadOnlyList<ReaderEvent> Events => _events;

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
        => new RecordingDataReader(result, this, RecordCommand(command));

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
        => new(new RecordingDataReader(result, this, RecordCommand(command)));

    private int RecordCommand(DbCommand command)
    {
        _commandTexts.Add(command.CommandText);
        return _commandTexts.Count - 1;
    }

    private void RecordRead(int commandIndex, int rowNumber, bool result)
        => _events.Add(new ReaderEvent(commandIndex, rowNumber, result));

    private sealed class RecordingDataReader(
        DbDataReader inner,
        RecordingReaderInterceptor interceptor,
        int commandIndex) : DbDataReader
    {
        private int _rowNumber;

        public override object this[int ordinal] => inner[ordinal];

        public override object this[string name] => inner[name];

        public override int Depth => inner.Depth;

        public override int FieldCount => inner.FieldCount;

        public override bool HasRows => inner.HasRows;

        public override bool IsClosed => inner.IsClosed;

        public override int RecordsAffected => inner.RecordsAffected;

        public override int VisibleFieldCount => inner.VisibleFieldCount;

        public override bool Read()
        {
            var result = inner.Read();
            interceptor.RecordRead(commandIndex, ++_rowNumber, result);
            return result;
        }

        public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
        {
            var result = await inner.ReadAsync(cancellationToken).ConfigureAwait(false);
            interceptor.RecordRead(commandIndex, ++_rowNumber, result);
            return result;
        }

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
