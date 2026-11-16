namespace GreenDonut.Data;

/// <summary>
/// One <see cref="System.Data.Common.DbDataReader.Read"/> or
/// <see cref="System.Data.Common.DbDataReader.ReadAsync()"/> call recorded by
/// <see cref="RecordingReaderInterceptor"/>.
/// </summary>
/// <param name="CommandIndex">
/// The zero-based index of the command this read belongs to, in execution order.
/// </param>
/// <param name="RowNumber">
/// The one-based number of this read call within its command.
/// </param>
/// <param name="Result">
/// The value the read call returned: true if a row was produced, false at the end of the result
/// set.
/// </param>
public sealed record ReaderEvent(int CommandIndex, int RowNumber, bool Result);
