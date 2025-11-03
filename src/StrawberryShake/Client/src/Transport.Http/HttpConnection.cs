using System.Text;
using System.Text.Json;
using HotChocolate.Transport.Http;
using static StrawberryShake.Properties.Resources;
using static StrawberryShake.Transport.Http.ResponseEnumerable;

namespace StrawberryShake.Transport.Http;

public class HttpConnection : IHttpConnection
{
    public const string RequestUri = "StrawberryShake.Transport.Http.HttpConnection.RequestUri";
    public const string HttpClient = "StrawberryShake.Transport.Http.HttpConnection.HttpClient";

    private readonly Func<OperationRequest, object?, HttpClient> _createClient;
    private readonly object? _clientFactoryState;

    public HttpConnection(Func<HttpClient> createClient)
    {
        ArgumentNullException.ThrowIfNull(createClient);

        _createClient = static (_, factory) => ((Func<HttpClient>)factory!).Invoke();
        _clientFactoryState = createClient;
    }

    public HttpConnection(Func<OperationRequest, object?, HttpClient> clientFactory, object? clientFactoryState = null)
    {
        ArgumentNullException.ThrowIfNull(clientFactory);

        _createClient = clientFactory;
        _clientFactoryState = clientFactoryState;
    }

    public IAsyncEnumerable<Response<JsonDocument>> ExecuteAsync(OperationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Create(
            CreateClient(request),
            CreateHttpRequest(request),
            CreateResponse);
    }

    protected virtual HttpClient CreateClient(OperationRequest request)
    {
        var contextData = request.GetContextDataOrNull();
        HttpClient? httpClient = null;
        Uri? requestUri = null;

        if (contextData is not null)
        {
            if (contextData.TryGetValue(RequestUri, out var value))
            {
                if (value is string stringValue)
                {
                    requestUri = new Uri(stringValue);
                }
                else if (value is Uri uriValue)
                {
                    requestUri = uriValue;
                }
            }

            if (contextData.TryGetValue(HttpClient, out value)
                && value is HttpClient httpClientValue)
            {
                httpClient = httpClientValue;
            }
        }

        httpClient ??= _createClient(request, _clientFactoryState);

        if (requestUri is not null)
        {
            httpClient.BaseAddress = requestUri;
        }

        return httpClient;
    }

    protected virtual GraphQLHttpRequest CreateHttpRequest(OperationRequest request)
    {
        var (id, name, document, variables, extensions, _, files, strategy) = request;

        var hasFiles = files is { Count: > 0 };

        variables = MapVariables(variables);
        if (hasFiles && variables is not null)
        {
            variables = MapFilesToVariables(variables, files!);
        }

        HotChocolate.Transport.OperationRequest operation;

        if (strategy == RequestStrategy.PersistedOperation)
        {
            operation = new HotChocolate.Transport.OperationRequest(null, id, name, onError: null, variables, extensions);
        }
        else
        {
            var body = Encoding.UTF8.GetString(document.Body);

            operation = new HotChocolate.Transport.OperationRequest(body, null, name, onError: null, variables, extensions);
        }

        return new GraphQLHttpRequest(operation) { EnableFileUploads = hasFiles };
    }

    protected virtual Response<JsonDocument> CreateResponse(
        HttpResponseContext responseContext)
    {
        return new Response<JsonDocument>(
            responseContext.Body,
            responseContext.Exception,
            responseContext.IsPatch,
            responseContext.HasNext,
            responseContext.Extensions,
            responseContext.ContextData);
    }

    /// <summary>
    /// Converts the variables into a dictionary that can be serialized. This is necessary
    /// because the variables can contain lists of key value pairs which are not supported
    /// by HotChocolate.Transport.Http
    /// </summary>
    /// <remarks>
    /// We only convert the variables if necessary to avoid unnecessary allocations.
    /// </remarks>
    protected static IReadOnlyDictionary<string, object?>? MapVariables(
        IReadOnlyDictionary<string, object?> variables)
    {
        if (variables.Count == 0)
        {
            return null;
        }

        Dictionary<string, object?>? copy = null;
        foreach (var variable in variables)
        {
            var value = variable.Value;
            // the value can be a List<T> of key value pairs and not only a dictionary. We do expect
            // to just have lists here, but in case we have a dictionary this should also just work.
            if (value is IEnumerable<KeyValuePair<string, object?>> items)
            {
                copy ??= new Dictionary<string, object?>(variables);

                value = MapVariables(new Dictionary<string, object?>(items));
            }
            else if (value is List<object?> list)
            {
                // the lists are mutable so we can just update the value in the list
                MapVariables(list);
            }

            if (copy is not null)
            {
                copy[variable.Key] = value;
            }
        }

        return copy ?? variables;
    }

    private static void MapVariables(List<object?> variables)
    {
        if (variables.Count == 0)
        {
            return;
        }

        for (var index = 0; index < variables.Count; index++)
        {
            switch (variables[index])
            {
                case IEnumerable<KeyValuePair<string, object?>> items:
                    variables[index] = MapVariables(new Dictionary<string, object?>(items));
                    break;

                case List<object?> list:
                    MapVariables(list);
                    break;
            }
        }
    }

    private static IReadOnlyDictionary<string, object?> MapFilesToVariables(
        IReadOnlyDictionary<string, object?> variables,
        IReadOnlyDictionary<string, Upload?> files)
    {
        foreach (var file in files)
        {
            var path = file.Key;
            var upload = file.Value;

            if (!upload.HasValue)
            {
                continue;
            }

            var currentPath = path["variables.".Length..];
            object? currentObject = variables;
            int index;
            while ((index = currentPath.IndexOf('.')) >= 0)
            {
                var segment = currentPath[..index];
                switch (currentObject)
                {
                    case Dictionary<string, object> dictionary:
                        if (!dictionary.TryGetValue(segment, out currentObject))
                        {
                            throw new InvalidOperationException(
                                string.Format(HttpConnection_FileMapDoesNotMatch, path));
                        }

                        break;

                    case List<object> array:
                        if (!int.TryParse(segment, out var arrayIndex))
                        {
                            throw new InvalidOperationException(
                                string.Format(HttpConnection_FileMapDoesNotMatch, path));
                        }

                        if (arrayIndex >= array.Count)
                        {
                            throw new InvalidOperationException(
                                string.Format(HttpConnection_FileMapDoesNotMatch, path));
                        }

                        currentObject = array[arrayIndex];
                        break;

                    default:
                        throw new InvalidOperationException(
                            string.Format(HttpConnection_FileMapDoesNotMatch, path));
                }

                currentPath = currentPath[(index + 1)..];
            }

            switch (currentObject)
            {
                case Dictionary<string, object> result:
                    result[currentPath] =
                        new FileReference(upload.Value.Content, upload.Value.FileName, upload.Value.ContentType);
                    break;

                case List<object> array:
                    if (!int.TryParse(currentPath, out var arrayIndex))
                    {
                        throw new InvalidOperationException(
                            string.Format(HttpConnection_FileMapDoesNotMatch, path));
                    }

                    if (arrayIndex >= array.Count)
                    {
                        throw new InvalidOperationException(
                            string.Format(HttpConnection_FileMapDoesNotMatch, path));
                    }

                    array[arrayIndex] =
                        new FileReference(upload.Value.Content, upload.Value.FileName, upload.Value.ContentType);

                    break;

                default:
                    throw new InvalidOperationException(
                        string.Format(HttpConnection_FileMapDoesNotMatch, path));
            }
        }

        return variables;
    }
}
