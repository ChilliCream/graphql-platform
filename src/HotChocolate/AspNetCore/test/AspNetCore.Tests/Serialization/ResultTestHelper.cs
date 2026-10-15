using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Text.Json;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// Creates results, batches and streams for formatter tests.
/// </summary>
internal static class ResultTestHelper
{
    private static readonly JsonSerializerOptions s_options = new(JsonSerializerDefaults.Web);

    public static OperationResultData CreateData(object value)
        => new(value, isValueNull: false, new DictionaryJsonFormatter(value), memoryHolder: null);

    public static OperationResult CreateResult(Dictionary<string, object?> data)
        => new(CreateData(data));

    /// <summary>
    /// Creates a result whose data is written by <paramref name="write"/>. An exception thrown
    /// by <paramref name="write"/> escapes from the formatter.
    /// </summary>
    public static OperationResult CreateResult(Action<JsonWriter> write)
        => new(new OperationResultData(write, isValueNull: false, new DelegateJsonFormatter(write), memoryHolder: null));

    public static ResponseStream CreateStream(params OperationResult[] results)
        => new(() => CreateResults(results), ExecutionResultKind.SubscriptionResult);

    /// <summary>
    /// Creates a stream that yields its first result only after <paramref name="gate"/> completed.
    /// </summary>
    public static ResponseStream CreateStream(Task gate, params OperationResult[] results)
        => new(() => CreateResults(gate, results), ExecutionResultKind.SubscriptionResult);

    public static async IAsyncEnumerable<OperationResult> CreateResults(IReadOnlyList<OperationResult> results)
    {
        foreach (var result in results)
        {
            yield return result;
            await Task.Yield();
        }
    }

    private static async IAsyncEnumerable<OperationResult> CreateResults(Task gate, IReadOnlyList<OperationResult> results)
    {
        await gate;

        foreach (var result in results)
        {
            yield return result;
            await Task.Yield();
        }
    }

    private sealed class DictionaryJsonFormatter(object value) : IRawJsonFormatter
    {
        public void WriteDataTo(JsonWriter jsonWriter)
            => JsonValueFormatter.WriteValue(jsonWriter, value, s_options);
    }

    private sealed class DelegateJsonFormatter(Action<JsonWriter> write) : IRawJsonFormatter
    {
        public void WriteDataTo(JsonWriter jsonWriter) => write(jsonWriter);
    }
}
