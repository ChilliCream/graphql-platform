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

    // Anchors of different apps join the model from concurrently starting gateways, and the
    // model resource collection is not thread safe.
    private static readonly Lock s_modelSync = new();

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

        // The composition resolves the schema settings directory from IProjectMetadata, which
        // only a project resource carries, so the settings of a JavaScript app are invisible
        // to it. The schema anchor bridges that gap: a project resource that exists only in
        // the application model, whose project metadata points at the package.json of the app
        // and which shares the endpoint annotations of the app. Through the anchor the
        // composition reads schema-settings.json, downloads the schema from the running app,
        // and routes the gateway to the allocated URL of the app, exactly like a .NET source
        // schema. Only the directory of the project metadata path is ever consumed.
        var anchor = new ProjectResource($"{app.Name}-schema");
        anchor.Annotations.Add(
            new SchemaAnchorProjectMetadata(IOPath.Combine(appDirectory, "package.json")));
        var anchorBuilder = builder.ApplicationBuilder.CreateResourceBuilder(anchor);

        var eventing = builder.ApplicationBuilder.Eventing;

        eventing.Subscribe<BeforeStartEvent>((beforeStart, _) =>
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
            // variable. The port pin from the settings URL is cosmetic (the composition
            // routes to the allocated endpoint either way) but keeps the dashboard URL
            // predictable. BeforeStartEvent completes before the orchestrator reads the
            // model, so the endpoint behaves like one declared in code.
            if (!app.Annotations.OfType<EndpointAnnotation>()
                .Any(endpoint => endpoint.UriScheme is "http" or "https"))
            {
                builder.WithHttpEndpoint(
                    port: environmentUrl?.IsLoopback is true ? environmentUrl.Port : null,
                    env: portEnvironmentVariable);
            }

            // The app may declare its HTTP endpoint under a custom name, and the anchor must
            // target that endpoint. The URL callback runs when the endpoints of the app are
            // allocated.
            var endpointName = app.Annotations.OfType<EndpointAnnotation>()
                .First(endpoint => endpoint.UriScheme is "http" or "https")
                .Name;
            builder.WithUrlForEndpoint(endpointName, url => url.Url += path);

            // The anchor carries the source schema annotation that the composition discovers.
            anchorBuilder.WithGraphQLHttpEndpoint(
                path,
                schemaPath,
                endpointName,
                sourceSchemaName);

            if (!settingsFound)
            {
                logger.LogWarning(
                    "Skipping GraphQL schema composition for {ResourceName}: {SettingsFile} not found.",
                    app.Name,
                    settingsFile);
                return Task.CompletedTask;
            }

            // The composition watches resources that carry a source schema for restarts, which
            // the app itself does not. The host annotation makes a restart of the app
            // recompose the gateways like a restart of a .NET source schema.
            if (!app.Annotations.OfType<GraphQLSourceSchemaHostAnnotation>().Any())
            {
                app.Annotations.Add(new GraphQLSourceSchemaHostAnnotation());
            }

            foreach (var gateway in gateways)
            {
                // The anchor joins the model only when a gateway that composes against the
                // app is about to start: after the orchestrator has read the resources it
                // starts, so the anchor never runs, and before the composition discovers
                // source schemas, because this subscription predates the composition's and
                // subscriptions run in order.
                eventing.Subscribe<BeforeResourceStartedEvent>(
                    gateway,
                    async (started, cancellationToken) =>
                    {
                        lock (s_modelSync)
                        {
                            AttachSchemaAnchor(anchor, app, gateway, beforeStart.Model);
                        }

                        // The composition waits for the resource that carries the source
                        // schema annotation to become healthy. The anchor never runs, so
                        // wait for the app instead and report the anchor healthy on its
                        // behalf.
                        var notifications = started.Services
                            .GetRequiredService<ResourceNotificationService>();

                        try
                        {
                            await notifications.WaitForResourceHealthyAsync(
                                app.Name,
                                WaitBehavior.StopOnResourceUnavailable,
                                cancellationToken);
                        }
                        catch (DistributedApplicationException exception)
                        {
                            throw new DistributedApplicationException(
                                $"The source schema resource '{app.Name}' required by "
                                + $"'{gateway.Name}' did not become healthy.",
                                exception);
                        }

                        await notifications.PublishUpdateAsync(anchor, snapshot => snapshot with
                        {
                            State = KnownResourceStates.Running,
                            IsHidden = true
                        });
                    });
            }

            return Task.CompletedTask;
        });

        return builder;
    }

    /// <summary>
    /// Makes the schema anchor of a JavaScript app part of the application model, mirrors the
    /// endpoints of the app on it, and lets the gateway reference it.
    /// </summary>
    internal static void AttachSchemaAnchor(
        ProjectResource anchor,
        IResource app,
        IResource gateway,
        DistributedApplicationModel model)
    {
        // Share the endpoint annotations of the app on purpose: the orchestrator assigns
        // AllocatedEndpoint to them when it allocates the endpoints of the app, and the
        // anchor reflects that automatically.
        foreach (var endpoint in app.Annotations.OfType<EndpointAnnotation>())
        {
            if (!anchor.Annotations.Contains(endpoint))
            {
                anchor.Annotations.Add(endpoint);
            }
        }

        if (!model.Resources.Contains(anchor))
        {
            model.Resources.Add(anchor);
        }

        if (!gateway.Annotations.OfType<ResourceRelationshipAnnotation>()
            .Any(relationship => ReferenceEquals(relationship.Resource, anchor)))
        {
            gateway.Annotations.Add(new ResourceRelationshipAnnotation(anchor, "Reference"));
        }
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

    private sealed class SchemaAnchorProjectMetadata(string projectPath) : IProjectMetadata
    {
        public string ProjectPath { get; } = projectPath;
    }
}
