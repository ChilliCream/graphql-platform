using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.Transport.InMemory;

/// <summary>
/// Represents a client for sending and receiving messaged to a local schema
/// </summary>
public class InMemoryClient : IInMemoryClient
{
    private readonly string _name;

    /// <summary>
    /// Initializes a new instance of a <see cref="InMemoryClient"/>
    /// </summary>
    /// <param name="name">
    /// The name of the client
    /// </param>
    public InMemoryClient(string name)
    {
        _name = !string.IsNullOrEmpty(name)
            ? name
            : throw ThrowHelper.Argument_IsNullOrEmpty(nameof(name));
    }

    /// <inheritdoc />
    public string SchemaName { get; set; } = ISchemaDefinition.DefaultName;

    /// <inheritdoc />
    public IRequestExecutor? Executor { get; set; }

    /// <inheritdoc />
    public IList<IInMemoryRequestInterceptor> RequestInterceptors { get; } = [];

    /// <inheritdoc />
    public string Name => _name;

    /// <inheritdoc />
    public async ValueTask<IExecutionResult> ExecuteAsync(
        OperationRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (Executor is null)
        {
            throw ThrowHelper.InMemoryClient_NoExecutorConfigured(_name);
        }

        var requestBuilder = new OperationRequestBuilder();

        if (request.Document.Body.Length > 0)
        {
            requestBuilder.SetDocument(Utf8GraphQLParser.Parse(request.Document.Body));
        }
        else
        {
            requestBuilder.SetDocumentId(request.Id);
        }

        requestBuilder.SetOperationName(request.Name);
        requestBuilder.SetVariableValues(CreateVariables(request, out var fileLookup));
        if (fileLookup is not null)
        {
            requestBuilder.Features.Set<IFileLookup>(fileLookup);
        }
        requestBuilder.SetExtensions(request.GetExtensionsOrNull());
        requestBuilder.SetGlobalState(request.GetContextDataOrNull());

        var applicationService = Executor.Schema.Services.GetRootServiceProvider();
        foreach (var interceptor in RequestInterceptors)
        {
            await interceptor
                .OnCreateAsync(applicationService, request, requestBuilder, cancellationToken)
                .ConfigureAwait(false);
        }

        return await Executor
            .ExecuteAsync(requestBuilder.Build(), cancellationToken)
            .ConfigureAwait(false);
    }

    private IReadOnlyDictionary<string, object?>? CreateVariables(
        OperationRequest request,
        out IFileLookup? fileLookup)
    {
        fileLookup = null;

        if (request.Variables is { } variables)
        {
            var unflattened = MapFilesToLookup(request.Files, out fileLookup);
            var response = new Dictionary<string, object?>();

            foreach (var pair in variables)
            {
                unflattened.TryGetValue(pair.Key, out var fileValue);
                response[pair.Key] = CreateVariableValue(pair.Value, fileValue);
            }

            return response;
        }

        return null;
    }

    private object? CreateVariableValue(object? variables, object? fileValue)
    {
        switch (variables)
        {
            case IEnumerable<KeyValuePair<string, object?>> pairs:
            {
                var response = new Dictionary<string, object?>();
                foreach (var pair in pairs)
                {
                    GetFileValueOrDefault(fileValue, pair.Key, out var currentFileValue);
                    response[pair.Key] = CreateVariableValue(pair.Value, currentFileValue);
                }

                return response;
            }
            case IList list:
            {
                var response = new List<object?>();
                for (var index = 0; index < list.Count; index++)
                {
                    GetFileValueOrDefault(fileValue, index, out var currentFileValue);
                    response.Add(CreateVariableValue(list[index], currentFileValue));
                }

                return response;
            }
            default:
                if (variables is null && fileValue is string fileKey)
                {
                    return fileKey;
                }

                return variables;
        }
    }

    private static void GetFileValueOrDefault(
        object? source,
        object key,
        out object? value)
    {
        value = (source, key) switch
        {
            (Dictionary<string, object?> s, string prop) when s.ContainsKey(prop) => s[prop],
            (List<object?> l, int i) when i < l.Count => l[i],
            _ => null
        };
    }

    private static IReadOnlyDictionary<string, object?> MapFilesToLookup(
        IReadOnlyDictionary<string, Upload?> files,
        out IFileLookup? fileLookup)
    {
        if (files.Count == 0)
        {
            fileLookup = null;
            return ImmutableDictionary<string, object?>.Empty;
        }

        var unflattened = new Dictionary<string, object?>();
        var fileMap = new Dictionary<string, IFile>();

        foreach (var file in files)
        {
            if (!file.Value.HasValue)
            {
                continue;
            }

            var upload = file.Value.Value;
            fileMap[file.Key] =
                new StreamFile(upload.FileName, () => upload.Content, null, upload.ContentType);

            object? current = unflattened;
            var path = file.Key.Split('.').ToArray();
            for (var i = 1; i < path.Length; i++)
            {
                var segment = path[i];

                var nextSegment = i + 1 == path.Length ? null : path[i + 1];

                if (char.IsDigit(segment[0]))
                {
                    if (current is not List<object?> currentList)
                    {
                        throw new InvalidOperationException(
                            "Path was invalid. Expected list but got object");
                    }

                    var index = int.Parse(segment);
                    while (currentList.Count <= index)
                    {
                        currentList.Add(null);
                    }

                    if (nextSegment is null)
                    {
                        currentList[index] = file.Key;
                    }
                    else if (currentList.ElementAtOrDefault(index) is not null)
                    {
                    }
                    else if (char.IsDigit(nextSegment[0]))
                    {
                        currentList[index] = new List<object?>();
                    }
                    else
                    {
                        currentList[index] = new Dictionary<string, object?>();
                    }

                    current = currentList[index];
                }
                else
                {
                    if (current is not Dictionary<string, object?> currentDict)
                    {
                        throw new InvalidOperationException(
                            "Path was invalid. Expected list but got object");
                    }

                    if (nextSegment is null)
                    {
                        currentDict[segment] = file.Key;
                    }
                    else if (currentDict.TryGetValue(segment, out var o) && o is not null)
                    {
                    }
                    else if (char.IsDigit(nextSegment[0]))
                    {
                        currentDict[segment] = new List<object?>();
                    }
                    else
                    {
                        currentDict[segment] = new Dictionary<string, object?>();
                    }

                    current = currentDict[segment];
                }
            }
        }

        fileLookup = fileMap.Count == 0 ? null : new InMemoryFileLookup(fileMap);
        return unflattened;
    }

    private sealed class InMemoryFileLookup(IReadOnlyDictionary<string, IFile> files) : IFileLookup
    {
        public bool TryGetFile(string name, [NotNullWhen(true)] out IFile? file)
        {
            if (files.TryGetValue(name, out var current))
            {
                file = current;
                return true;
            }

            file = null;
            return false;
        }
    }
}
