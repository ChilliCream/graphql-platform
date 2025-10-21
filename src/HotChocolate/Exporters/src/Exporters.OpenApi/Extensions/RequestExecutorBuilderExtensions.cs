using System.Diagnostics.CodeAnalysis;
using HotChocolate;
using HotChocolate.AspNetCore;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Exporters.OpenApi;
using HotChocolate.Language;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

// TODO: Also add Fusion variants
public static class RequestExecutorBuilderExtensions
{
    public static IRequestExecutorBuilder AddOpenApiDocumentStorage(
        this IRequestExecutorBuilder builder,
        IOpenApiDocumentStorage documentStorage)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(documentStorage);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(s => s.AddSingleton(documentStorage));
    }

    public static IRequestExecutorBuilder AddOpenApiDocumentStorage<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>(
        this IRequestExecutorBuilder builder)
        where T : class, IOpenApiDocumentStorage
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddOpenApiDocumentStorageCore();

        return builder.ConfigureSchemaServices(static s => s.AddSingleton<IOpenApiDocumentStorage, T>());
    }

    private static void AddOpenApiDocumentStorageCore(this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddKeyedSingleton<DynamicEndpointDataSource>(builder.Name);

        builder.Services
            .AddHttpContextAccessor()
            .AddKeyedSingleton(
                builder.Name,
                // TODO: Maybe we should use a named one here to avoid conflicts in the future
                static (sp, name) => new HttpRequestExecutorProxy(
                    sp.GetRequiredService<IRequestExecutorProvider>(),
                    sp.GetRequiredService<IRequestExecutorEvents>(),
                    (string)name));

        builder.ConfigureSchemaServices(
            static (applicationServices, s) =>
                s.AddSingleton(schemaServices =>
                {
                    var schemaName = schemaServices.GetRequiredService<ISchemaDefinition>().Name;
                    return applicationServices.GetRequiredKeyedService<DynamicEndpointDataSource>(schemaName);
                }));

        builder.AddWarmupTask<OpenApiWarmupTask>();
    }
}

// TODO: Make this nicer and independent from executor lifetime
internal sealed class OpenApiWarmupTask(
    IOpenApiDocumentStorage storage,
    DynamicEndpointDataSource dynamicEndpointDataSource) : IRequestExecutorWarmupTask
{
    public bool ApplyOnlyOnStartup => true;

    public async Task WarmupAsync(IRequestExecutor executor, CancellationToken cancellationToken)
    {
        var documents = await storage.GetDocumentsAsync(cancellationToken);

        // TODO: eww
        var executableDocuments = documents
            .Select(x => x.Document.Definitions.FirstOrDefault(d => d is OperationDefinitionNode))
            .Where(d => d != null)
            .Cast<OperationDefinitionNode>()
            .Select(CreateExecutableDocument);

        dynamicEndpointDataSource.SetEndpoints(executableDocuments);
    }

    private ExecutableOpenApiDocument CreateExecutableDocument(OperationDefinitionNode operation)
    {
        var httpDirective = operation.Directives.First(d => d.Name.Value == WellKnownDirectiveNames.Http);

        var httpMethodValue = httpDirective.Arguments
            .First(x => x.Name.Value == WellKnownArgumentNames.Method).Value;
        var routeValue = httpDirective.Arguments
            .First(x => x.Name.Value == WellKnownArgumentNames.Route).Value;

        var rootField = operation.SelectionSet.Selections.OfType<FieldNode>().First();
        var responseNameToExtract = rootField.Alias?.Value ?? rootField.Name.Value;

        var document = new DocumentNode([operation.WithDirectives([])]);

        return new ExecutableOpenApiDocument(
            document,
            ParseHttpMethod(httpMethodValue),
            ParseRoute(routeValue),
            responseNameToExtract);
    }

    private static RoutePattern ParseRoute(IValueNode value)
    {
        if (value is not StringValueNode stringValue)
        {
            throw new ArgumentException("Expected string value");
        }

        return RoutePatternFactory.Parse(stringValue.Value);
    }

    private static string ParseHttpMethod(IValueNode value)
    {
        if (value is not EnumValueNode enumValue)
        {
            throw new ArgumentException("Expected enum value");
        }

        // TODO: Validate it's actually HttpMethods
        return enumValue.Value;
    }
}
