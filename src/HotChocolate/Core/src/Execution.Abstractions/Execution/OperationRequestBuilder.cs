using System.Diagnostics.CodeAnalysis;
using HotChocolate.Features;
using HotChocolate.Language;
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
    private IReadOnlyList<IReadOnlyDictionary<string, object?>>? _readOnlyVariableValues;
    private List<IReadOnlyDictionary<string, object?>>? _variableValues;
    private IReadOnlyDictionary<string, object?>? _readOnlyExtensions;
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
    /// <param name="documentHash"></param>
    /// <returns></returns>
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
    /// Sets the variable values for the GraphQL request.
    /// </summary>
    /// <param name="variableValues">
    /// The variable values for the GraphQL request.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    public OperationRequestBuilder AddVariableValues(
        IReadOnlyDictionary<string, object?> variableValues)
    {
        if (_readOnlyVariableValues is not null)
        {
            _variableValues = _readOnlyVariableValues.ToList();
            _readOnlyVariableValues = null;
        }

        _variableValues ??= [];
        _variableValues!.Add(variableValues);
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
        IReadOnlyDictionary<string, object?>? variableValues)
    {
        if (variableValues is null)
        {
            _variableValues = null;
            _readOnlyVariableValues = null;
        }
        else
        {
            _variableValues = [variableValues];
            _readOnlyVariableValues = null;
        }
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
    public OperationRequestBuilder SetVariableValuesSet(
        IReadOnlyList<IReadOnlyDictionary<string, object?>>? variableValues)
    {
        if (variableValues is null)
        {
            _variableValues = null;
            _readOnlyVariableValues = null;
        }
        else
        {
            _variableValues = null;
            _readOnlyVariableValues = variableValues;
        }
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
        IReadOnlyDictionary<string, object?>? extensions)
    {
        _readOnlyExtensions = extensions;
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
    public OperationRequestBuilder SetGlobalState(IReadOnlyDictionary<string, object?>? contextData)
    {
        _readOnlyContextData = _contextData;
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
        _readOnlyVariableValues = null;
        _variableValues = null;
        _readOnlyExtensions = null;
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
        IOperationRequest? request;

        var variableSet = GetVariableValues();
        var features = _features;

        if (features is null || features.IsEmpty)
        {
            features = FeatureCollection.Empty;
        }

        if (variableSet is { Count: > 1 })
        {
            request = new VariableBatchRequest(
                document: _document,
                documentId: _documentId,
                documentHash: _documentHash,
                operationName: _operationName,
                variableValues: variableSet,
                extensions: _readOnlyExtensions,
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
            variableValues: variableSet is { Count: 1 }
                ? variableSet[0]
                : null,
            extensions: _readOnlyExtensions,
            contextData: _readOnlyContextData ?? _contextData,
            features: features,
            services: _services,
            flags: _flags
        );
        Reset();
        return request;
    }

    private IReadOnlyList<IReadOnlyDictionary<string, object?>>? GetVariableValues()
        => _variableValues ?? _readOnlyVariableValues;

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
                    _readOnlyVariableValues = batch.VariableValues,
                    _readOnlyContextData = batch.ContextData,
                    _readOnlyExtensions = batch.Extensions,
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
                    _readOnlyVariableValues = operation.VariableValues is not null
                        ? new List<IReadOnlyDictionary<string, object?>>(1) { operation.VariableValues }
                        : null,
                    _readOnlyContextData = operation.ContextData,
                    _readOnlyExtensions = operation.Extensions,
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
            .SetVariableValuesSet(request.Variables)
            .SetExtensions(request.Extensions);

        if (request.Document is not null)
        {
            builder.SetDocument(request.Document);
        }

        return builder;
    }
}
