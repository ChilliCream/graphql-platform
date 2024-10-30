using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using static HotChocolate.Properties.AbstractionResources;

namespace HotChocolate.Execution;

public sealed class OperationRequestBuilder
{
    private IOperationDocument? _document;
    private OperationDocumentId? _documentId;
    private string? _documentHash;
    private string? _operationName;
    private IReadOnlyList<IReadOnlyDictionary<string, object?>>? _readOnlyVariableValues;
    private List<IReadOnlyDictionary<string, object?>>? _variableValues;
    private IReadOnlyDictionary<string, object?>? _readOnlyExtensions;
    private Dictionary<string, object?>? _contextData;
    private IReadOnlyDictionary<string, object?>? _readOnlyContextData;
    private IServiceProvider? _services;
    private GraphQLRequestFlags _flags = GraphQLRequestFlags.AllowAll;

    /// <summary>
    /// Sets the GraphQL operation document that shall be executed.
    /// </summary>
    /// <param name="sourceText">
    /// The GraphQL operation document source text.
    /// </param>
    /// <returns>
    /// Returns this instance of <see cref="OperationRequestBuilder" /> for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="sourceText"/> is <c>null</c> or empty.
    /// </exception>
    public OperationRequestBuilder SetDocument([StringSyntax("graphql")] string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
        {
            throw new ArgumentException(
                OperationRequestBuilder_OperationIsNullOrEmpty,
                nameof(sourceText));
        }

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
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

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
    public OperationRequestBuilder SetDocumentHash(string? documentHash)
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
            _variableValues = new List<IReadOnlyDictionary<string, object?>>(1) { variableValues, };
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

        _contextData ??= new Dictionary<string, object?>();
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

        _contextData ??= new Dictionary<string, object?>();
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

        _contextData ??= new Dictionary<string, object?>();

        if (!_contextData.ContainsKey(name))
        {
            _contextData.Add(name, value);
        }
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
    public OperationRequestBuilder SetFlags(GraphQLRequestFlags flags)
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
        _flags = GraphQLRequestFlags.AllowAll;
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

        if (variableSet is { Count: > 1, })
        {
            request = new VariableBatchRequest(
                document: _document,
                documentId: _documentId,
                documentHash: _documentHash,
                operationName: _operationName,
                variableValues: variableSet,
                contextData: _readOnlyContextData ?? _contextData,
                extensions: _readOnlyExtensions,
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
            variableValues: variableSet is { Count: 1, }
                ? variableSet[0]
                : null,
            contextData: _readOnlyContextData ?? _contextData,
            extensions: _readOnlyExtensions,
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
                    _flags = batch.Flags,
                },
            OperationRequest operation
                => new OperationRequestBuilder
                {
                    _document = operation.Document,
                    _documentId = operation.DocumentId,
                    _documentHash = operation.DocumentHash,
                    _operationName = operation.OperationName,
                    _readOnlyVariableValues = operation.VariableValues is not null
                        ? new List<IReadOnlyDictionary<string, object?>>(1) { operation.VariableValues, }
                        : null,
                    _readOnlyContextData = operation.ContextData,
                    _readOnlyExtensions = operation.Extensions,
                    _services = operation.Services,
                    _flags = operation.Flags,
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
            .SetDocumentId(request.QueryId)
            .SetDocumentHash(request.QueryHash)
            .SetOperationName(request.OperationName)
            .SetVariableValuesSet(request.Variables)
            .SetExtensions(request.Extensions);

        if (request.Query is not null)
        {
            builder.SetDocument(request.Query);
        }

        return builder;
    }
}
