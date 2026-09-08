using System.Buffers;
using System.Text.Json;

namespace HotChocolate.Fusion.Policies.Rego;

/// <summary>
/// Merges the Rego data documents contributed by the FAR package and the registered data providers
/// into the single data document the policy set is compiled with.
/// </summary>
internal static class RegoDataMerge
{
    /// <summary>
    /// Merges the given UTF-8 encoded JSON documents as a collision-rejecting top-level union: every
    /// document must have a JSON object as its root, and a top-level key defined by more than one
    /// document is rejected rather than overridden.
    /// </summary>
    /// <param name="documents">
    /// The documents to merge, in the order their top-level keys are written to the result.
    /// </param>
    /// <returns>The merged document as newly allocated UTF-8 encoded JSON.</returns>
    /// <exception cref="RegoDataMergeException">
    /// A document is not valid JSON, a document's root is not a JSON object, or two documents
    /// define the same top-level key.
    /// </exception>
    public static byte[] Merge(IReadOnlyList<ReadOnlyMemory<byte>> documents)
    {
        var merged = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        var openedDocuments = new List<JsonDocument>(documents.Count);

        try
        {
            foreach (var document in documents)
            {
                JsonDocument parsed;

                try
                {
                    parsed = JsonDocument.Parse(document);
                }
                catch (JsonException ex)
                {
                    throw new RegoDataMergeException("A Rego data document is not valid JSON.", ex);
                }

                openedDocuments.Add(parsed);

                if (parsed.RootElement.ValueKind != JsonValueKind.Object)
                {
                    throw new RegoDataMergeException(
                        "A Rego data document must have a JSON object as its root.");
                }

                foreach (var property in parsed.RootElement.EnumerateObject())
                {
                    if (!merged.TryAdd(property.Name, property.Value))
                    {
                        throw new RegoDataMergeException(
                            $"The top-level key '{property.Name}' is defined by more than one "
                            + "Rego data source.");
                    }
                }
            }

            var buffer = new ArrayBufferWriter<byte>();

            using (var writer = new Utf8JsonWriter(buffer))
            {
                writer.WriteStartObject();

                foreach (var (key, value) in merged)
                {
                    writer.WritePropertyName(key);
                    value.WriteTo(writer);
                }

                writer.WriteEndObject();
            }

            return buffer.WrittenSpan.ToArray();
        }
        finally
        {
            foreach (var document in openedDocuments)
            {
                document.Dispose();
            }
        }
    }
}
