using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// This class is used to configure the request executor.
/// </summary>
public sealed class RequestExecutorSetup
{
    private readonly List<OnConfigureSchemaBuilderAction> _onConfigureSchemaBuilderHooks = [];
    private readonly List<OnConfigureRequestExecutorOptionsAction> _onConfigureRequestExecutorOptionsHooks = [];
    private readonly List<RequestCoreMiddleware> _pipeline = [];
    private readonly List<OnConfigureSchemaServices> _onConfigureSchemaServicesHooks = [];
    private readonly List<OnRequestExecutorCreatedAction> _onRequestExecutorCreatedHooks = [];
    private readonly List<OnRequestExecutorEvictedAction> _onRequestExecutorEvictedHooks = [];
    private readonly List<ITypeModule> _typeModules = [];

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
    public IList<OnConfigureRequestExecutorOptionsAction> OnConfigureRequestExecutorOptionsHooks
        => _onConfigureRequestExecutorOptionsHooks;

    /// <summary>
    /// Gets the schema service configuration actions.
    /// This hook is invoked second in the schema creation process.
    /// </summary>
    public IList<OnConfigureSchemaServices> OnConfigureSchemaServicesHooks
        => _onConfigureSchemaServicesHooks;

    /// <summary>
    /// Gets the schema builder configuration actions.
    /// This hook is invoked third in the schema creation process.
    /// </summary>
    public IList<OnConfigureSchemaBuilderAction> OnConfigureSchemaBuilderHooks
        => _onConfigureSchemaBuilderHooks;

    /// <summary>
    /// Gets the request executor created actions.
    /// This hook is invoked fourth in the schema creation process.
    /// </summary>
    public IList<OnRequestExecutorCreatedAction> OnRequestExecutorCreatedHooks
        => _onRequestExecutorCreatedHooks;

    /// <summary>
    /// Gets the request executor evicted actions.
    /// This hook is invoked when a request executor is phased out.
    /// </summary>
    public IList<OnRequestExecutorEvictedAction> OnRequestExecutorEvictedHooks
        => _onRequestExecutorEvictedHooks;

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
    /// Gets or sets the default pipeline factory.
    /// </summary>
    public Action<IList<RequestCoreMiddleware>>? DefaultPipelineFactory { get; set; }

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
        options._onConfigureSchemaBuilderHooks.AddRange(_onConfigureSchemaBuilderHooks);
        options._onConfigureRequestExecutorOptionsHooks.AddRange(_onConfigureRequestExecutorOptionsHooks);
        options._pipeline.AddRange(_pipeline);
        options._onConfigureSchemaServicesHooks.AddRange(_onConfigureSchemaServicesHooks);
        options._onRequestExecutorCreatedHooks.AddRange(_onRequestExecutorCreatedHooks);
        options._onRequestExecutorEvictedHooks.AddRange(_onRequestExecutorEvictedHooks);
        options._typeModules.AddRange(_typeModules);

        if(DefaultPipelineFactory is not null)
        {
            options.DefaultPipelineFactory = DefaultPipelineFactory;
        }
    }
}

public delegate void OnConfigureSchemaServices(
    ConfigurationContext context,
    IServiceCollection services);
