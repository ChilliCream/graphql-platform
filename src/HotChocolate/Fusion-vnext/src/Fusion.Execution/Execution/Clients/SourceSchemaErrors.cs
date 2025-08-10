using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaErrors
{
    /// <summary>
    /// Errors without a path.
    /// </summary>
    public required List<IError>? RootErrors { get; init; }

    public required ErrorTrie Trie { get; init; }

    public static SourceSchemaErrors? From(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        List<IError>? rootErrors = null;
        ErrorTrie root = new ErrorTrie();

        foreach (var jsonError in json.EnumerateArray())
        {
            var currentTrie = root;

            var error = CreateError(jsonError);

            if (error is null)
            {
                continue;
            }

            if (error.Path is null)
            {
                rootErrors ??= [];
                rootErrors.Add(error);
                continue;
            }

            var pathSegments = error.Path.ToList();
            var lastPathIndex = pathSegments.Count - 1;

            for (var i = 0; i < pathSegments.Count; i++)
            {
                var pathSegment = pathSegments[i];

                if (currentTrie.TryGetValue(pathSegment, out var trieAtPath))
                {
                    currentTrie = trieAtPath;
                }
                else
                {
                    var newTrie = new ErrorTrie();
                    currentTrie[pathSegment] = newTrie;
                    currentTrie = newTrie;
                }

                if (i == lastPathIndex)
                {
                    currentTrie.Error = error;
                }
            }
        }

        return new SourceSchemaErrors { RootErrors = rootErrors, Trie = root };
    }

    private static IError? CreateError(JsonElement jsonError)
    {
        if (jsonError.ValueKind is not JsonValueKind.Object)
        {
            return null;
        }

        if (jsonError.TryGetProperty("message", out var message)
            && message.ValueKind is JsonValueKind.String)
        {
            var errorBuilder = ErrorBuilder.New()
                .SetMessage(message.GetString()!);

            if (jsonError.TryGetProperty("path", out var path) && path.ValueKind == JsonValueKind.Array)
            {
                errorBuilder.SetPath(CreatePathFromJson(path));
            }

            if (jsonError.TryGetProperty("code", out var code)
                && code.ValueKind is JsonValueKind.String)
            {
                errorBuilder.SetCode(code.GetString());
            }

            if (jsonError.TryGetProperty("extensions", out var extensions)
                && extensions.ValueKind is JsonValueKind.Object)
            {
                foreach (var property in extensions.EnumerateObject())
                {
                    errorBuilder.SetExtension(property.Name, property.Value);
                }
            }

            return errorBuilder.Build();
        }

        return null;
    }

    private static Path CreatePathFromJson(JsonElement errorSubPath)
    {
        var path = Path.Root;

        for (var i = 0; i < errorSubPath.GetArrayLength(); i++)
        {
            path = errorSubPath[i] switch
            {
                { ValueKind: JsonValueKind.String } nameElement => path.Append(nameElement.GetString()!),
                { ValueKind: JsonValueKind.Number } indexElement => path.Append(indexElement.GetInt32()),
                _ => throw new InvalidOperationException("The error path contains an unsupported element."),
            };
        }

        return path;
    }
}
