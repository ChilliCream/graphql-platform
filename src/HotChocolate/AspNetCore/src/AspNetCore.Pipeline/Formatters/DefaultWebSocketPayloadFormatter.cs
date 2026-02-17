using System.Text.Json;
using HotChocolate.Text.Json;
using HotChocolate.Transport.Formatters;
using static HotChocolate.Execution.JsonValueFormatter;

namespace HotChocolate.AspNetCore.Formatters;

/// <summary>
/// This represents the default implementation for the <see cref="IWebSocketPayloadFormatter" />.
/// </summary>
public sealed class DefaultWebSocketPayloadFormatter(WebSocketPayloadFormatterOptions options = default)
    : IWebSocketPayloadFormatter
{
    private readonly JsonSerializerOptions _serializerOptions = options.Json.CreateSerializerOptions();
    private readonly JsonResultFormatter _internalFormatter = new(options.Json);
    private readonly JsonNullIgnoreCondition _nullIgnoreCondition = options.Json.NullIgnoreCondition;

    /// <inheritdoc />
    public void Format(OperationResult result, JsonWriter writer)
    {
        // Save the writer's current null ignore condition so we can restore it after formatting.
        var savedNullIgnoreCondition = writer.NullIgnoreCondition;

        try
        {
            // Apply the null ignore condition configured for this payload formatter.
            writer.NullIgnoreCondition = _nullIgnoreCondition;
            _internalFormatter.Format(result, writer);
        }
        finally
        {
            // Restore the original null ignore condition.
            writer.NullIgnoreCondition = savedNullIgnoreCondition;
        }
    }

    /// <inheritdoc />
    public void Format(IError error, JsonWriter writer)
    {
        // Save the writer's current null ignore condition so we can restore it after formatting.
        var savedNullIgnoreCondition = writer.NullIgnoreCondition;

        try
        {
            // Apply the null ignore condition configured for this payload formatter.
            writer.NullIgnoreCondition = _nullIgnoreCondition;
            WriteError(writer, error, _serializerOptions);
        }
        finally
        {
            // Restore the original null ignore condition.
            writer.NullIgnoreCondition = savedNullIgnoreCondition;
        }
    }

    /// <inheritdoc />
    public void Format(IReadOnlyList<IError> errors, JsonWriter writer)
    {
        // Save the writer's current null ignore condition so we can restore it after formatting.
        var savedNullIgnoreCondition = writer.NullIgnoreCondition;

        try
        {
            // Apply the null ignore condition configured for this payload formatter.
            writer.NullIgnoreCondition = _nullIgnoreCondition;

            writer.WriteStartArray();

            for (var i = 0; i < errors.Count; i++)
            {
                WriteError(writer, errors[i], _serializerOptions);
            }

            writer.WriteEndArray();
        }
        finally
        {
            // Restore the original null ignore condition.
            writer.NullIgnoreCondition = savedNullIgnoreCondition;
        }
    }

    /// <inheritdoc />
    public void Format(IReadOnlyDictionary<string, object?> extensions, JsonWriter writer)
    {
        // Save the writer's current null ignore condition so we can restore it after formatting.
        var savedNullIgnoreCondition = writer.NullIgnoreCondition;

        try
        {
            // Apply the null ignore condition configured for this payload formatter.
            writer.NullIgnoreCondition = _nullIgnoreCondition;
            WriteDictionary(writer, extensions, _serializerOptions);
        }
        finally
        {
            // Restore the original null ignore condition.
            writer.NullIgnoreCondition = savedNullIgnoreCondition;
        }
    }
}
