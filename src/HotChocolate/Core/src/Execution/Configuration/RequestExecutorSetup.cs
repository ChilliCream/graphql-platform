using System;
using System.Collections.Generic;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// This class is used to configure the request executor.
/// </summary>
public sealed class RequestExecutorSetup
{
    private readonly List<SchemaBuilderAction> _schemaBuilderActions = new();
    private readonly List<RequestExecutorOptionsAction> _requestExecutorOptionsActions = new();
    private readonly List<RequestCoreMiddleware> _pipeline = new();
    private readonly List<Action<IServiceCollection>> _schemaServices = new();
    private readonly List<OnRequestExecutorCreatedAction> _onRequestExecutorCreated = new();
    private readonly List<OnRequestExecutorEvictedAction> _onRequestExecutorEvicted = new();
    private readonly List<ITypeModule> _typeModules = new();

    /// <summary>
    /// This allows to specify a schema and short-circuit the schema creation.
    /// </summary>
    public ISchema? Schema { get; set; }

    /// <summary>
    /// Gets or sets the schema builder that is used to create the schema.
    /// </summary>
    public ISchemaBuilder? SchemaBuilder { get; set; }

    /// <summary>
    /// Gets or sets the request executor options.
    /// </summary>
    public RequestExecutorOptions? RequestExecutorOptions { get; set; }

    /// <summary>
    /// Gets the request executor options actions.
    /// This hook is invoke first in the schema creation process.
    /// </summary>
    public IList<RequestExecutorOptionsAction> RequestExecutorOptionsActions
        => _requestExecutorOptionsActions;

    /// <summary>
    /// Gets the schema service configuration actions.
    /// This hook is invoked second in the schema creation process.
    /// </summary>
    public IList<Action<IServiceCollection>> SchemaServices
        => _schemaServices;

    /// <summary>
    /// Gets the schema builder configuration actions.
    /// This hook is invoked third in the schema creation process.
    /// </summary>
    public IList<SchemaBuilderAction> SchemaBuilderActions
        => _schemaBuilderActions;

    /// <summary>
    /// Gets the request executor created actions.
    /// This hook is invoked fourth in the schema creation process.
    /// </summary>
    public IList<OnRequestExecutorCreatedAction> OnRequestExecutorCreated
        => _onRequestExecutorCreated;

    /// <summary>
    /// Gets the request executor evicted actions.
    /// This hook is invoked when a request executor is phased out.
    /// </summary>
    public IList<OnRequestExecutorEvictedAction> OnRequestExecutorEvicted
        => _onRequestExecutorEvicted;

    /// <summary>
    /// Gets the type modules that are used to configure the schema.
    /// </summary>
    public IList<ITypeModule> TypeModules
        => _typeModules;

    /// <summary>
    /// Gets the middleware that make up the request pipeline.
    /// </summary>
    public IList<RequestCoreMiddleware> Pipeline
        => _pipeline;

    /// <summary>
    /// Copies the options to the specified other options object.
    /// </summary>
    /// <param name="options">
    /// The options object to which the options are copied to.
    /// </param>
    public void CopyTo(RequestExecutorSetup options)
    {
        options.Schema = Schema;
        options.SchemaBuilder = SchemaBuilder;
        options.RequestExecutorOptions = RequestExecutorOptions;
        options._schemaBuilderActions.AddRange(_schemaBuilderActions);
        options._requestExecutorOptionsActions.AddRange(_requestExecutorOptionsActions);
        options._pipeline.AddRange(_pipeline);
        options._schemaServices.AddRange(_schemaServices);
        options._onRequestExecutorCreated.AddRange(_onRequestExecutorCreated);
        options._onRequestExecutorEvicted.AddRange(_onRequestExecutorEvicted);
        options._typeModules.AddRange(_typeModules);
    }
}
