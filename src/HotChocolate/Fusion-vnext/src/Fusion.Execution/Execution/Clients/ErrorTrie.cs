using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

// TODO: Add docs
public sealed class ErrorTrie : Dictionary<object, ErrorTrie>
{
    public JsonElement Error { get; private set; }

    public static ErrorTrie? From(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var root = new ErrorTrie();

        foreach (var error in json.EnumerateArray())
        {
            var currentTrie = root;

            if (!error.TryGetProperty("path", out var path) || path.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var pathSegment in path.EnumerateArray())
            {
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
            }

            currentTrie.Error = error;
        }

        return root;
    }
}
