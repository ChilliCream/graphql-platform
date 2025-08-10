using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaErrors
{
    /// <summary>
    /// Errors without a path.
    /// </summary>
    public required List<JsonElement>? RootErrors { get; init; }

    public required ErrorTrie Trie { get; init; }

    public static SourceSchemaErrors? From(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<JsonElement>? rootErrors = null;
        ErrorTrie root = new ErrorTrie();

        foreach (var error in json.EnumerateArray())
        {
            var currentTrie = root;

            if (!error.TryGetProperty("path", out var path) || path.ValueKind != JsonValueKind.Array)
            {
                rootErrors ??= [];
                rootErrors.Add(error);
                continue;
            }

            for (int i = 0, len = path.GetArrayLength(); i < len; ++i)
            {
                var pathSegment = path[i];
                object? pathSegmentValue = pathSegment.ValueKind switch
                {
                    JsonValueKind.String => pathSegment.GetString(),
                    JsonValueKind.Number => pathSegment.GetInt32(),
                    _ => null
                };

                if (pathSegmentValue is null)
                {
                    break;
                }

                if (currentTrie.TryGetValue(pathSegmentValue, out var trieAtPath))
                {
                    currentTrie = trieAtPath;
                }
                else
                {
                    var newTrie = new ErrorTrie();
                    currentTrie[pathSegmentValue] = newTrie;
                    currentTrie = newTrie;
                }

                if (i == len - 1)
                {
                    currentTrie.Error = error;
                }
            }
        }

        return new SourceSchemaErrors { RootErrors = rootErrors, Trie = root };
    }
}
