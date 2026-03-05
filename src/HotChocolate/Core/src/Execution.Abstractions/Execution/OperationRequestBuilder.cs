using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Text.Json;
using static HotChocolate.ExecutionAbstractionsResources;

namespace HotChocolate.Execution;

/// <summary>
/// Represents a builder for creating GraphQL operation requests.
/// </summary>
public sealed class OperationRequestBuilder : IFeatureProvider
{
    private IOperationDocument? _document;
    private OperationDocumentId? _documentId;
    private OperationDocumentHash? _documentHash;
    private string? _operationName;
    private JsonDocumentOwner? _variableValues;
    private JsonDocumentOwner? _extensions;
    private ErrorHandlingMode? _errorHandlingMode;
    private Dictionary<string, object?>? _contextData;
    private IReadOnlyDictionary<string, object?>? _readOnlyContextData;
    private IServiceProvider? _services;
    private RequestFlags _flags = RequestFlags.AllowAll;
    private IFeatureCollection? _features;

    /// <summary>
    /// Gets the operation request features.
    /// </summary>
    public IFeatureCollection Features { get => _features ??= new FeatureCollection(); }

    /// <summary>
    /// Sets the GraphQL operation document that shall be executed.
    /// </summary>
    /// <param name="sourceText">
    /// The GraphQL operation-document source text.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceText"/> is <c>null</c> or empty.
    /// </exception>
    public OperationRequestBuilder SetDocument([StringSyntax("graphql")] string sourceText)
    {
        ArgumentException.ThrowIfNullOrEmpty(sourceText);

        _document = new OperationDocumentSourceText(sourceText);
        return this;
    }

    /// <summary>
    /// Sets the GraphQL operation document that shall be executed.
    /// </summary>
    /// <param name="document">
    /// The parsed GraphQL operation document.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="document"/> is <c>null</c>.
    /// </exception>
    public OperationRequestBuilder SetDocument(DocumentNode document)
    {
        ArgumentNullException.ThrowIfNull(document);
        _document = new OperationDocument(document);
        return this;
    }

    /// <summary>
    /// Sets the GraphQL operation document id.
    /// </summary>
    /// <param name="documentId">
    /// The GraphQL operation document id.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetDocumentId(OperationDocumentId? documentId)
    {
        _documentId = documentId;
        return this;
    }

    /// <summary>
    /// Sets the hash of the GraphQL operation document.
    /// </summary>
    /// <param name="documentHash">
    /// The hash of the GraphQL operation document.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetDocumentHash(OperationDocumentHash? documentHash)
    {
        _documentHash = documentHash;
        return this;
    }

    /// <summary>
    /// Sets the name of the operation in the GraphQL request document that shall be executed.
    /// </summary>
    /// <param name="operationName">
    /// The name of the GraphQL operation within the GraphQL operation document.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetOperationName(string? operationName)
    {
        _operationName = operationName;
        return this;
    }

    /// <summary>
    /// Sets the requested error handling mode.
    /// </summary>
    /// <param name="errorHandlingMode">
    /// The requested error handling mode.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetErrorHandlingMode(ErrorHandlingMode? errorHandlingMode)
    {
        _errorHandlingMode = errorHandlingMode;
        return this;
    }

    /// <summary>
    /// Sets the variable values for the GraphQL request.
    /// </summary>
    /// <param name="variableValues">
    /// The variable values for the GraphQL request.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetVariableValues(
        [StringSyntax("json")] string variableValues)
    {
        ArgumentException.ThrowIfNullOrEmpty(variableValues);
        return SetVariableValues(JsonDocument.Parse(variableValues));
    }

    /// <summary>
    /// Sets the variable values for the GraphQL request.
    /// </summary>
    /// <param name="variableValues">
    /// The variable values for the GraphQL request.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetVariableValues(
        JsonDocument? variableValues)
    {
        if (variableValues is null)
        {
            _variableValues?.Dispose();
            _variableValues = null;
            return this;
        }

        if (variableValues.RootElement.ValueKind is JsonValueKind.Null)
        {
            variableValues.Dispose();
            _variableValues?.Dispose();
            _variableValues = null;
            return this;
        }

        if (variableValues.RootElement.ValueKind is not (JsonValueKind.Object or JsonValueKind.Array))
        {
            throw new ArgumentException(
                OperationRequestBuilder_SetVariableValues_JSONDocumentMustBeObjectOrArray,
                nameof(variableValues));
        }

        _variableValues?.Dispose();
        _variableValues = new JsonDocumentOwner(variableValues);
        return this;
    }

    public OperationRequestBuilder SetVariableValues(
        IEnumerable<KeyValuePair<string, JsonElement>>? variableValues)
    {
        _variableValues?.Dispose();
        _variableValues = null;

        if (variableValues is null)
        {
            return this;
        }

        var buffer = new PooledArrayWriter();

        try
        {
            using (var jsonWriter = new Utf8JsonWriter(buffer))
            {
                jsonWriter.WriteStartObject();
                foreach (var (name, value) in variableValues)
                {
                    jsonWriter.WritePropertyName(name);
                    value.WriteTo(jsonWriter);
                }
                jsonWriter.WriteEndObject();
            }

            var document = JsonDocument.Parse(buffer.WrittenMemory);
            _variableValues = new JsonDocumentOwner(document, buffer);
            return this;
        }
        catch
        {
            buffer.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Sets the variable values for the GraphQL request.
    /// The dictionary will be serialized to JSON internally.
    /// </summary>
    /// <param name="variableValues">
    /// The variable values for the GraphQL request as a dictionary.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    public OperationRequestBuilder SetVariableValues(IReadOnlyDictionary<string, object?>? variableValues)
    {
        _variableValues?.Dispose();
        _variableValues = null;

        if (variableValues is null)
        {
            return this;
        }

        _variableValues = new(JsonSerializer.SerializeToDocument(variableValues, JsonSerializerOptionDefaults.GraphQL));
        return this;
    }

    /// <summary>
    /// Sets the variable values for the GraphQL request as a batch operation.
    /// Each dictionary in the list represents a set of variables for a separate operation execution.
    /// The list will be serialized to JSON internally.
    /// </summary>
    /// <param name="variableValueSets">
    /// The list of variable value sets for batch GraphQL request execution.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    public OperationRequestBuilder SetVariableValues(
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableValueSets)
    {
        _variableValues?.Dispose();
        _variableValues = null;

        if (variableValueSets is null)
        {
            return this;
        }

        _variableValues = new(JsonSerializer.SerializeToDocument(variableValueSets, JsonSerializerOptionDefaults.GraphQL));
        return this;
    }

    /// <summary>
    /// Sets the GraphQL request extension data.
    /// </summary>
    /// <param name="extensions">
    /// The GraphQL request extension data.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetExtensions(
        JsonDocument? extensions)
    {
        _extensions?.Dispose();
        _extensions = null;

        if (extensions is null)
        {
            return this;
        }

        _extensions = new(extensions);
        return this;
    }

    /// <summary>
    /// Sets the GraphQL request extension data.
    /// </summary>
    /// <param name="extensions">
    /// The GraphQL request extension data.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    [RequiresDynamicCode("JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
    [RequiresUnreferencedCode("JSON serialization and deserialization might require types that cannot be statically analyzed. Use the overload that takes a JsonTypeInfo or JsonSerializerContext, or make sure all of the required types are preserved.")]
    public OperationRequestBuilder SetExtensions(IReadOnlyDictionary<string, object?>? extensions)
    {
        _extensions?.Dispose();
        _extensions = null;

        if (extensions is null)
        {
            return this;
        }

        _extensions = new(JsonSerializer.SerializeToDocument(extensions, JsonSerializerOptionDefaults.GraphQL));
        return this;
    }

    /// <summary>
    /// Sets the initial global request state.
    /// </summary>
    /// <param name="contextData">
    /// The initial global request state.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetGlobalState(
        IReadOnlyDictionary<string, object?>? contextData)
    {
        _readOnlyContextData = contextData;
        _contextData = null;
        return this;
    }

    /// <summary>
    /// Sets the initial global request state.
    /// </summary>
    /// <param name="name">
    /// The name of the global state.
    /// </param>
    /// <param name="value">
    /// The value of the global state.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetGlobalState(string name, object? value)
    {
        if (_readOnlyContextData is not null)
        {
            _contextData = _readOnlyContextData.ToDictionary(t => t.Key, t => t.Value);
            _readOnlyContextData = null;
        }

        _contextData ??= [];
        _contextData[name] = value;
        return this;
    }

    /// <summary>
    /// Adds a global state to the initial global request state.
    /// </summary>
    /// <param name="name">
    /// The name of the global state.
    /// </param>
    /// <param name="value">
    /// The value of the global state.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder AddGlobalState(string name, object? value)
    {
        if (_readOnlyContextData is not null)
        {
            _contextData = _readOnlyContextData.ToDictionary(t => t.Key, t => t.Value);
            _readOnlyContextData = null;
        }

        _contextData ??= [];
        _contextData.Add(name, value);
        return this;
    }

    /// <summary>
    /// Tries to add a global state to the initial global request state.
    /// </summary>
    /// <param name="name">
    /// The name of the global state.
    /// </param>
    /// <param name="value">
    /// The value of the global state.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder TryAddGlobalState(string name, object? value)
    {
        if (_readOnlyContextData is not null)
        {
            _contextData = _readOnlyContextData.ToDictionary(t => t.Key, t => t.Value);
            _readOnlyContextData = null;
        }

        _contextData ??= [];
        _contextData.TryAdd(name, value);
        return this;
    }

    /// <summary>
    /// Removes a global state from the initial global request state.
    /// </summary>
    /// <param name="name">
    /// The name of the global state.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder RemoveGlobalState(string name)
    {
        if (_readOnlyContextData is not null)
        {
            _contextData = _readOnlyContextData.ToDictionary(t => t.Key, t => t.Value);
            _readOnlyContextData = null;
        }

        if (_contextData is null)
        {
            return this;
        }

        _contextData.Remove(name);
        return this;
    }

    /// <summary>
    /// Sets the initial global request state.
    /// </summary>
    /// <param name="services">
    /// The services that shall be used while executing the GraphQL request.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetServices(IServiceProvider? services)
    {
        _services = services;
        return this;
    }

    /// <summary>
    /// Tries to set the initial global request state.
    /// </summary>
    /// <param name="services">
    /// The services that shall be used while executing the GraphQL request.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder TrySetServices(IServiceProvider? services)
    {
        _services ??= services;
        return this;
    }

    /// <summary>
    /// Sets the GraphQL request flags can be used to limit the execution engine capabilities.
    /// </summary>
    /// <param name="flags">
    /// The GraphQL request flags can be used to limit the execution engine capabilities.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder SetFlags(RequestFlags flags)
    {
        _flags = flags;
        return this;
    }

    /// <summary>
    /// Resets the builder to its initial state.
    /// </summary>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder Reset()
    {
        _document = null;
        _documentId = null;
        _documentHash = null;
        _operationName = null;
        _errorHandlingMode = null;
        _variableValues = null;
        _extensions = null;
        _contextData = null;
        _readOnlyContextData = null;
        _services = null;
        _features = null;
        _flags = RequestFlags.AllowAll;
        return this;
    }

    /// <summary>
    /// Builds the operation request.
    /// </summary>
    /// <returns>
    /// Returns the operation request.
    /// </returns>
    public IOperationRequest Build()
    {
        if (_document is null && OperationDocumentId.IsNullOrEmpty(_documentId))
        {
            throw new InvalidOperationException(OperationRequest_DocumentOrIdMustBeSet);
        }

        IOperationRequest? request;

        var features = _features;

        if (features is null || features.IsEmpty)
        {
            features = FeatureCollection.Empty;
        }

        if (_variableValues?.Document.RootElement.ValueKind is JsonValueKind.Array)
        {
            request = new VariableBatchRequest(
                document: _document,
                documentId: _documentId,
                documentHash: _documentHash,
                operationName: _operationName,
                errorHandlingMode: _errorHandlingMode,
                variableValues: _variableValues,
                extensions: _extensions,
                contextData: _readOnlyContextData ?? _contextData,
                features: features,
                services: _services,
                flags: _flags);
            Reset();
            return request;
        }

        request = new OperationRequest(
            document: _document,
            documentId: _documentId,
            documentHash: _documentHash,
            operationName: _operationName,
            errorHandlingMode: _errorHandlingMode,
            variableValues: _variableValues,
            extensions: _extensions,
            contextData: _readOnlyContextData ?? _contextData,
            features: features,
            services: _services,
            flags: _flags
        );
        Reset();
        return request;
    }

    /// <summary>
    /// Creates a new instance of <see cref="OperationRequestBuilder" />.
    /// </summary>
    /// <returns></returns>
    public static OperationRequestBuilder New() => new();

    /// <summary>
    /// Creates a new instance of <see cref="OperationRequestBuilder" /> from an existing request.
    /// </summary>
    /// <param name="request">
    /// The existing request from which the new builder is created.
    /// </param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">
    /// The request type is not supported.
    /// </exception>
    public static OperationRequestBuilder From(IOperationRequest request)
        => request switch
        {
            VariableBatchRequest batch
                => new OperationRequestBuilder
                {
                    _document = batch.Document,
                    _documentId = batch.DocumentId,
                    _documentHash = batch.DocumentHash,
                    _operationName = batch.OperationName,
                    _errorHandlingMode = batch.ErrorHandlingMode,
                    _variableValues = batch.VariableValues,
                    _readOnlyContextData = batch.ContextData,
                    _extensions = batch.Extensions,
                    _services = batch.Services,
                    _flags = batch.Flags
                },
            OperationRequest operation
                => new OperationRequestBuilder
                {
                    _document = operation.Document,
                    _documentId = operation.DocumentId,
                    _documentHash = operation.DocumentHash,
                    _operationName = operation.OperationName,
                    _errorHandlingMode = operation.ErrorHandlingMode,
                    _variableValues = operation.VariableValues,
                    _readOnlyContextData = operation.ContextData,
                    _extensions = operation.Extensions,
                    _services = operation.Services,
                    _flags = operation.Flags
                },
            _ => throw new NotSupportedException("The request type is not supported.")
        };

    /// <summary>
    /// Creates a new instance of <see cref="OperationRequestBuilder" /> from an existing request.
    /// </summary>
    /// <param name="request">
    /// The existing request from which the new builder is created.
    /// </param>
    /// <returns>
    /// Returns a new instance of <see cref="OperationRequestBuilder" />.
    /// </returns>
    public static OperationRequestBuilder From(GraphQLRequest request)
    {
        var builder = New();

        builder
            .SetDocumentId(request.DocumentId)
            .SetDocumentHash(request.DocumentHash)
            .SetOperationName(request.OperationName)
            .SetErrorHandlingMode(request.ErrorHandlingMode)
            .SetVariableValues(request.Variables)
            .SetExtensions(request.Extensions);

        if (request.Document is not null)
        {
            builder.SetDocument(request.Document);
        }

        return builder;
    }
}
