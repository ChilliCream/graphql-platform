using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class SourceSchemaErrors
{
    /// <summary>
    /// Errors without a path.
    /// </summary>
    public required ImmutableArray<IError> RootErrors { get; init; }

    public required ErrorTrie Trie { get; init; }

    public static SourceSchemaErrors? From(JsonElement json)
    {
        if (json.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        ImmutableArray<IError>.Builder? rootErrors = null;
        var root = new ErrorTrie();

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
                rootErrors ??= ImmutableArray.CreateBuilder<IError>();
                rootErrors.Add(error);
                continue;
            }

            var rented = ArrayPool<object>.Shared.Rent(error.Path.Length);
            var pathSegments = rented.AsSpan(0, error.Path.Length);
            error.Path.ToList(pathSegments);
            var lastPathIndex = pathSegments.Length - 1;

            try
            {
                for (var i = 0; i < pathSegments.Length; i++)
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
            finally
            {
                pathSegments.Clear();
                ArrayPool<object>.Shared.Return(rented);
            }
        }

        return new SourceSchemaErrors { RootErrors = rootErrors?.ToImmutableArray() ?? [], Trie = root };
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
