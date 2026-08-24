using System.Text.Json;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.JavaScript;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IOPath = System.IO.Path;

namespace HotChocolate.Fusion.Aspire;

/// <summary>
/// Provides extension methods that let a JavaScript app expose a GraphQL source schema.
/// </summary>
public static class GraphQLJavaScriptAppResourceBuilderExtensions
{
    private static readonly string[] s_urlPropertyPreference = ["devUrl", "url"];

    /// <summary>
    /// Marks a JavaScript app as exposing a GraphQL endpoint over HTTP, so that the app
    /// participates in the GraphQL schema composition like a .NET source schema. The schema
    /// composition reads the <c>schema-settings.json</c> next to the app's <c>package.json</c>
    /// and downloads the schema from the running app.
    /// </summary>
    /// <param name="builder">The resource builder</param>
    /// <param name="path">
    /// The path of the GraphQL endpoint that the app serves. It must start with '/'.
    /// </param>
    /// <param name="schemaPath">
    /// The path the schema document is downloaded from. It must start with '/' and must not be
    /// <c>null</c> unless the source schema uses Apollo Federation, which serves its schema
    /// through the GraphQL endpoint at <paramref name="path"/> and ignores this path.
    /// </param>
    /// <param name="portEnvironmentVariable">
    /// The name of the environment variable that passes the port to bind to the app (defaults
    /// to "PORT"). It only applies when the app does not already declare an HTTP endpoint.
    /// </param>
    /// <param name="sourceSchemaName">
    /// An optional source schema name assertion. When specified, it must exactly match the
    /// <c>name</c> in <c>schema-settings.json</c>.
    /// </param>
    /// <returns>The resource builder for chaining</returns>
    [AspireExport("withJavaScriptAppGraphQLHttpEndpoint", MethodName = "withGraphQLHttpEndpoint")]
    public static IResourceBuilder<JavaScriptAppResource> WithGraphQLHttpEndpoint(
        this IResourceBuilder<JavaScriptAppResource> builder,
        string path = "/graphql",
        string? schemaPath = "/graphql/schema.graphql",
        string portEnvironmentVariable = "PORT",
        string? sourceSchemaName = null)
    {
        if (!path.StartsWith('/'))
        {
            throw new ArgumentException(
                "The GraphQL endpoint path must start with '/'.",
                nameof(path));
        }

        if (schemaPath?.StartsWith('/') is false)
        {
            throw new ArgumentException(
                "The GraphQL schema endpoint path must start with '/'.",
                nameof(schemaPath));
        }

        var app = builder.Resource;
        var appDirectory = IOPath.GetFullPath(
            app.WorkingDirectory,
            builder.ApplicationBuilder.AppHostDirectory);

        // This subscription predates the composition's, so the annotations exist before the
        // composition discovers source schemas.
        builder.ApplicationBuilder.Eventing.Subscribe<BeforeStartEvent>((beforeStart, _) =>
        {
            var logger = beforeStart.Services.GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(GraphQLJavaScriptAppResourceBuilderExtensions).FullName!);

            var gateways = beforeStart.Model.GetGraphQLCompositionResources()
                .Where(gateway => SchemaComposition
                    .GetReferencedResources(gateway, beforeStart.Model)
                    .Contains(app))
                .ToList();

            // The port of the app derives from the transport URL that the settings declare
            // for the environment the gateway composes against.
            var settingsFile = IOPath.Combine(appDirectory, "schema-settings.json");
            var settingsFound = File.Exists(settingsFile);
            Uri? environmentUrl = null;

            if (settingsFound && gateways.Count > 0)
            {
                environmentUrl = ResolveEnvironmentUrl(
                    settingsFile,
                    GetCompositionEnvironmentName(gateways[0]),
                    logger);
            }

            // A GraphQL source schema needs an HTTP endpoint. Declare one unless the app
            // already has one, and tell the app where to bind through the given environment
            // variable. BeforeStartEvent completes before the orchestrator reads the model,
            // so the endpoint behaves like one declared in code.
            if (!app.Annotations.OfType<EndpointAnnotation>()
                .Any(endpoint => endpoint.UriScheme is "http" or "https"))
            {
                builder.WithHttpEndpoint(
                    port: environmentUrl is { IsLoopback: true, IsDefaultPort: false }
                        ? environmentUrl.Port
                        : null,
                    env: portEnvironmentVariable);
            }

            // The app may declare its HTTP endpoint under a custom name, and the source
            // schema annotation must target that endpoint. The URL callback runs when the
            // endpoints of the app are allocated.
            var endpointName = app.Annotations.OfType<EndpointAnnotation>()
                .First(endpoint => endpoint.UriScheme is "http" or "https")
                .Name;
            builder.WithUrlForEndpoint(endpointName, url => url.Url += path);

            if (!settingsFound)
            {
                logger.LogWarning(
                    "Skipping GraphQL schema composition for {ResourceName}: {SettingsFile} not found.",
                    app.Name,
                    settingsFile);
                return Task.CompletedTask;
            }

            // The composition reads the settings of a source schema from its project
            // directory, which a JavaScript app does not have, so the annotation names the
            // app directory instead.
            builder.WithAnnotation(
                new GraphQLSourceSchemaAnnotation
                {
                    SourceSchemaName = sourceSchemaName,
                    EndpointName = endpointName,
                    SchemaPath = schemaPath,
                    GraphQLPath = path,
                    Location = SourceSchemaLocationType.SchemaEndpoint
                });
            builder.WithAnnotation(new GraphQLSourceSchemaDirectoryAnnotation(appDirectory));

            return Task.CompletedTask;
        });

        return builder;
    }

    /// <summary>
    /// Resolves the HTTP transport URL that <c>schema-settings.json</c> declares for the given
    /// settings environment. Like the composition, the development URL is preferred over the
    /// configured URL.
    /// </summary>
    internal static Uri? ResolveEnvironmentUrl(
        string settingsFile,
        string environmentName,
        ILogger logger)
    {
        try
        {
            using var settings = JsonDocument.Parse(File.ReadAllText(settingsFile));
            var root = settings.RootElement;

            if (root.ValueKind is not JsonValueKind.Object
                || !root.TryGetProperty("transports", out var transports)
                || transports.ValueKind is not JsonValueKind.Object
                || !transports.TryGetProperty("http", out var http)
                || http.ValueKind is not JsonValueKind.Object)
            {
                return null;
            }

            foreach (var urlProperty in s_urlPropertyPreference)
            {
                if (http.TryGetProperty(urlProperty, out var value)
                    && value.ValueKind is JsonValueKind.String
                    && value.GetString() is { } template
                    && !string.IsNullOrWhiteSpace(template)
                    && SettingsComposer.TryResolveVariables(
                        template,
                        root,
                        environmentName,
                        out var resolved)
                    && Uri.TryCreate(resolved, UriKind.Absolute, out var url))
                {
                    return url;
                }
            }

            return null;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Could not resolve the {EnvironmentName} environment URL from {SettingsFile}.",
                environmentName,
                settingsFile);
            return null;
        }
    }

    private static string GetCompositionEnvironmentName(IResource gateway)
        => gateway.GetCompositionSettings()?.Settings.EnvironmentName ?? "Aspire";
}
