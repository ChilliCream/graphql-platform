using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Execution.Internal;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Pipeline;
using HotChocolate.Stitching.Requests;
using HotChocolate.Stitching.SchemaDefinitions;
using HotChocolate.Stitching.Utilities;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;
using static HotChocolate.Stitching.ThrowHelper;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class HotChocolateStitchingRequestExecutorExtensions
{
    /// <summary>
    /// This middleware delegates GraphQL requests to a different GraphQL server using
    /// GraphQL HTTP requests.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder UseHttpRequests(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.UseRequest<HttpRequestMiddleware>();
    }

    public static IRequestExecutorBuilder UseHttpRequestPipeline(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder
            .UseInstrumentation()
            .UseExceptions()
            .UseDocumentCache()
            .UseDocumentParser()
            .UseDocumentValidation()
            .UseOperationCache()
            .UseOperationResolver()
            .UseOperationVariableCoercion()
            .UseHttpRequests();
    }

    public static IRequestExecutorBuilder AddRemoteSchema(
        this IRequestExecutorBuilder builder,
        string schemaName,
        bool ignoreRootTypes = false)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        return AddRemoteSchema(
            builder,
            schemaName,
            async (services, cancellationToken) =>
            {
                // The schema will be fetched via HTTP from the downstream service.
                // We will use the schema name to get a the HttpClient, which
                // we expect is correctly configured.
                var httpClient = services
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(schemaName);

                // Next we will fetch the schema definition which contains the
                // schema document and other configuration
                return await new IntrospectionHelper(httpClient, schemaName)
                    .GetSchemaDefinitionAsync(cancellationToken)
                    .ConfigureAwait(false);
            },
            ignoreRootTypes);
    }

    public static IRequestExecutorBuilder AddRemoteSchemaFromString(
        this IRequestExecutorBuilder builder,
        string schemaName,
        string schemaSdl,
        bool ignoreRootTypes = false)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        return AddRemoteSchema(
            builder,
            schemaName,
            (_, _) =>
                new ValueTask<RemoteSchemaDefinition>(
                    new RemoteSchemaDefinition(
                        schemaName,
                        Utf8GraphQLParser.Parse(schemaSdl))),
            ignoreRootTypes);
    }

    public static IRequestExecutorBuilder AddRemoteSchemaFromFile(
        this IRequestExecutorBuilder builder,
        string schemaName,
        string fileName,
        bool ignoreRootTypes = false)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        return AddRemoteSchema(
            builder,
            schemaName,
            async (_, cancellationToken) =>
            {
                var schemaSdl = await File
                    .ReadAllBytesAsync(fileName, cancellationToken)
                    .ConfigureAwait(false);

                return new RemoteSchemaDefinition(
                    schemaName,
                    Utf8GraphQLParser.Parse(schemaSdl));
            },
            ignoreRootTypes);
    }

    public static IRequestExecutorBuilder AddRemoteSchema(
        this IRequestExecutorBuilder builder,
        string schemaName,
        Func<IServiceProvider, CancellationToken, ValueTask<RemoteSchemaDefinition>> loadSchema,
        bool ignoreRootTypes = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (loadSchema is null)
        {
            throw new ArgumentNullException(nameof(loadSchema));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        // first we add a full GraphQL schema and executor that represents the remote schema.
        // This remote schema will be used by the stitching engine to execute queries against
        // this schema and also to lookup types in order correctly convert between scalars.
        builder
            .AddGraphQL(schemaName)
            .ConfigureSchemaServices(services =>
            {
                services.TryAddSingleton(
                    sp => new HttpRequestClient(
                        sp.GetCombinedServices().GetRequiredService<IHttpClientFactory>(),
                        sp.GetRequiredService<IErrorHandler>(),
                        sp.GetCombinedServices()
                            .GetRequiredService<IHttpStitchingRequestInterceptor>()));

                services.TryAddSingleton<
                    IHttpStitchingRequestInterceptor,
                    HttpStitchingRequestInterceptor>();
            })
            .ConfigureSchemaAsync(
                async (services, schemaBuilder, cancellationToken) =>
                {
                    // No we need to load the schema document.
                    var schemaDef =
                        await loadSchema(services, cancellationToken)
                            .ConfigureAwait(false);

                    var document = schemaDef.Document.RemoveBuiltInTypes();

                    // We store the schema definition on the schema building context
                    // and copy it to the schema once that is built.
                    schemaBuilder
                        .SetContextData(typeof(RemoteSchemaDefinition).FullName!, schemaDef)
                        .TryAddTypeInterceptor<CopySchemaDefinitionTypeInterceptor>();

                    // The document is used to create a SDL-first schema ...
                    schemaBuilder.AddDocument(document);

                    // ... which will fail if any resolver is actually used ...
                    schemaBuilder.Use(_ => _ => throw new NotSupportedException());
                })
            // ... instead we are using a special request pipeline that does everything like
            // the standard pipeline except the last middleware will not start the execution
            // algorithms but delegate the request via HTTP to the downstream schema.
            .UseHttpRequestPipeline();

        // Next, we will register a request executor proxy with the stitched schema,
        // that the stitching runtime will use to send requests to the schema representing
        // the downstream service.
        builder
            .ConfigureSchemaAsync(async (services, schemaBuilder, cancellationToken) =>
            {
                var noLockExecutorResolver =
                    services.GetRequiredService<IInternalRequestExecutorResolver>();

                var executor = await noLockExecutorResolver
                    .GetRequestExecutorNoLockAsync(schemaName, cancellationToken)
                    .ConfigureAwait(false);

                var autoProxy = AutoUpdateRequestExecutorProxy.Create(
                    new RequestExecutorProxy(
                        services.GetRequiredService<IRequestExecutorResolver>(),
                        schemaName),
                    executor);

                schemaBuilder
                    .AddRemoteExecutor(schemaName, autoProxy)
                    .TryAddTypeInterceptor<StitchingSchemaInterceptor>()
                    .TryAddTypeInterceptor<StitchingTypeInterceptor>();

                var schemaDefinition =
                    (RemoteSchemaDefinition)autoProxy.Schema
                        .ContextData[typeof(RemoteSchemaDefinition).FullName!]!;

                var extensionsRewriter = new SchemaExtensionsRewriter();

                foreach (var extensionDocument in schemaDefinition.ExtensionDocuments)
                {
                    var doc = (DocumentNode)extensionsRewriter.Rewrite(
                        extensionDocument, new(schemaName))!;

                    var schemaExtension =
                        doc.Definitions.OfType<SchemaExtensionNode>().FirstOrDefault();

                    if (schemaExtension is not null &&
                        schemaExtension.Directives.Count == 0 &&
                        schemaExtension.OperationTypes.Count == 0)
                    {
                        var definitions = doc.Definitions.ToList();
                        definitions.Remove(schemaExtension);
                        doc = doc.WithDefinitions(definitions);
                    }

                    schemaBuilder.AddTypeExtensions(doc);
                }

                schemaBuilder.AddTypeRewriter(
                    new RemoveFieldRewriter(
                        new FieldReference(
                            autoProxy.Schema.QueryType.Name,
                            SchemaDefinitionFieldNames.SchemaDefinitionField),
                        schemaName));

                schemaBuilder.AddDocumentRewriter(
                    new RemoveTypeRewriter(
                        SchemaDefinitionType.Names.SchemaDefinition,
                        schemaName));

                foreach (var schemaAction in extensionsRewriter.SchemaActions)
                {
                    switch (schemaAction.Name.Value)
                    {
                        case DirectiveNames.RemoveRootTypes:
                            schemaBuilder.AddDocumentRewriter(
                                new RemoveRootTypeRewriter(schemaName));
                            break;

                        case DirectiveNames.RemoveType:
                            schemaBuilder.AddDocumentRewriter(
                                new RemoveTypeRewriter(
                                    GetArgumentValue(
                                        schemaAction,
                                        DirectiveFieldNames.RemoveType_TypeName),
                                    schemaName));
                            break;

                        case DirectiveNames.RenameType:
                            schemaBuilder.AddTypeRewriter(
                                new RenameTypeRewriter(
                                    GetArgumentValue(
                                        schemaAction,
                                        DirectiveFieldNames.RenameType_TypeName),
                                    GetArgumentValue(
                                        schemaAction,
                                        DirectiveFieldNames.RenameType_NewTypeName),
                                    schemaName));
                            break;

                        case DirectiveNames.RenameField:
                            schemaBuilder.AddTypeRewriter(
                                new RenameFieldRewriter(
                                    new FieldReference(
                                        GetArgumentValue(
                                            schemaAction,
                                            DirectiveFieldNames.RenameField_TypeName),
                                        GetArgumentValue(
                                            schemaAction,
                                            DirectiveFieldNames.RenameField_FieldName)),
                                    GetArgumentValue(
                                        schemaAction,
                                        DirectiveFieldNames.RenameField_NewFieldName),
                                    schemaName));
                            break;
                    }
                }
            });

        // Last but not least, we will setup the stitching context which will
        // provide access to the remote executors which in turn use the just configured
        // request executor proxies to send requests to the downstream services.
        builder.Services.TryAddScoped<IStitchingContext, StitchingContext>();

        if (ignoreRootTypes)
        {
            builder.AddDocumentRewriter(new RemoveRootTypeRewriter(schemaName));
        }

        return builder;
    }

    public static IRequestExecutorBuilder AddLocalSchema(
        this IRequestExecutorBuilder builder,
        string schemaName,
        bool ignoreRootTypes = false)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        schemaName.EnsureGraphQLName(nameof(schemaName));

        // Next, we will register a request executor proxy with the stitched schema,
        // that the stitching runtime will use to send requests to the schema representing
        // the downstream service.
        builder
            .ConfigureSchemaAsync(async (services, schemaBuilder, cancellationToken) =>
            {
                var noLockExecutorResolver =
                    services.GetRequiredService<IInternalRequestExecutorResolver>();

                var executor = await noLockExecutorResolver
                    .GetRequestExecutorNoLockAsync(schemaName, cancellationToken)
                    .ConfigureAwait(false);

                var autoProxy = AutoUpdateRequestExecutorProxy.Create(
                    new RequestExecutorProxy(
                        services.GetRequiredService<IRequestExecutorResolver>(),
                        schemaName),
                    executor);

                schemaBuilder
                    .AddRemoteExecutor(schemaName, autoProxy)
                    .TryAddTypeInterceptor<StitchingSchemaInterceptor>()
                    .TryAddTypeInterceptor<StitchingTypeInterceptor>();

                schemaBuilder.AddTypeRewriter(
                    new RemoveFieldRewriter(
                        new FieldReference(
                            autoProxy.Schema.QueryType.Name,
                            SchemaDefinitionFieldNames.SchemaDefinitionField),
                        schemaName));

                schemaBuilder.AddDocumentRewriter(
                    new RemoveTypeRewriter(
                        SchemaDefinitionType.Names.SchemaDefinition,
                        schemaName));
            });

        // Last but not least, we will setup the stitching context which will
        // provide access to the remote executors which in turn use the just configured
        // request executor proxies to send requests to the downstream services.
        builder.Services.TryAddScoped<IStitchingContext, StitchingContext>();

        if (ignoreRootTypes)
        {
            builder.AddDocumentRewriter(new RemoveRootTypeRewriter(schemaName));
        }

        return builder;
    }

    /// <summary>
    /// Add a type merge rule in order to define how a type is merged.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="mergeRuleFactory">
    /// A factory that create the type merging rule.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="mergeRuleFactory"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeMergeRule(
        this IRequestExecutorBuilder builder,
        MergeTypeRuleFactory mergeRuleFactory)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (mergeRuleFactory == null)
        {
            throw new ArgumentNullException(nameof(mergeRuleFactory));
        }

        return builder.ConfigureSchema(
            s => s.AddTypeMergeRules(mergeRuleFactory));
    }

    /// <summary>
    /// Add a directive merge rule in order to define
    /// how a directive is merged.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="mergeRuleFactory">
    /// A factory that create the directive merging rule.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="mergeRuleFactory"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddDirectiveMergeRule(
        this IRequestExecutorBuilder builder,
        MergeDirectiveRuleFactory mergeRuleFactory)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (mergeRuleFactory == null)
        {
            throw new ArgumentNullException(nameof(mergeRuleFactory));
        }

        return builder.ConfigureSchema(
            s => s.AddDirectiveMergeRules(mergeRuleFactory));
    }

    /// <summary>
    /// Add a type definition rewriter in order to rewrite a
    /// type definition on a remote schema document before
    /// it is being merged.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="rewriter">
    /// The type definition rewriter.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="rewriter"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeRewriter(
        this IRequestExecutorBuilder builder,
        ITypeRewriter rewriter)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rewriter == null)
        {
            throw new ArgumentNullException(nameof(rewriter));
        }

        return builder.ConfigureSchema(s => s.AddTypeRewriter(rewriter));
    }

    /// <summary>
    /// Add a document rewriter in order to rewrite
    /// a remote schema document before it is being merged.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="rewriter">
    /// The document rewriter.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="rewriter"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddDocumentRewriter(
        this IRequestExecutorBuilder builder,
        IDocumentRewriter rewriter)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rewriter == null)
        {
            throw new ArgumentNullException(nameof(rewriter));
        }

        return builder.ConfigureSchema(
            s => s.AddDocumentRewriter(rewriter));
    }

    /// <summary>
    /// Adds a schema SDL that contains type extensions.
    /// Extension documents can be used to extend merged types
    /// or even replace them.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="schemaSdl">
    /// The GraphQL schema SDL.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="schemaSdl"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeExtensionsFromString(
        this IRequestExecutorBuilder builder,
        string schemaSdl)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(schemaSdl))
        {
            throw new ArgumentNullException(nameof(schemaSdl));
        }

        return builder.ConfigureSchema(
            s => s.AddTypeExtensions(Utf8GraphQLParser.Parse(schemaSdl)));
    }

    /// <summary>
    /// Adds a schema SDL that contains type extensions.
    /// Extension documents can be used to extend merged types
    /// or even replace them.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="fileName">
    /// The file name of the type extension document.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="fileName"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeExtensionsFromFile(
        this IRequestExecutorBuilder builder,
        string fileName)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (string.IsNullOrEmpty(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        return builder.ConfigureSchemaAsync(
            async (s, ct) =>
            {
#if NETSTANDARD2_0
                    byte[] content = await Task
                        .Run(() => File.ReadAllBytes(fileName), ct)
                        .ConfigureAwait(false);
#else
                var content = await File
                    .ReadAllBytesAsync(fileName, ct)
                    .ConfigureAwait(false);
#endif

                s.AddTypeExtensions(Utf8GraphQLParser.Parse(content));
            });
    }

    /// <summary>
    /// Adds a schema SDL that contains type extensions.
    /// Extension documents can be used to extend merged types
    /// or even replace them.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="assembly">
    /// The assembly from which the type extension file shall be resolved.
    /// </param>
    /// <param name="key">
    /// The resource key of the type extension file
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="assembly"/> is <c>null</c>.
    /// <paramref name="key"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddTypeExtensionsFromResource(
        this IRequestExecutorBuilder builder,
        Assembly assembly,
        string key)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (assembly is null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        if (key is null)
        {
            throw new ArgumentNullException(nameof(key));
        }

        return builder.ConfigureSchemaAsync(
            async (s, ct) =>
            {
                var stream = assembly.GetManifestResourceStream(key);

                if (stream is null)
                {
                    throw RequestExecutorBuilder_ResourceNotFound(key);
                }

                await using (stream)
                {
                    var buffer = new byte[stream.Length];
                    await stream.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false);
                    s.AddTypeExtensions(Utf8GraphQLParser.Parse(buffer));
                }
            });
    }

    /// <summary>
    /// Add a document rewriter that is executed on
    /// the merged schema document.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="rewrite">
    /// A delegate that is called to execute the
    /// rewrite document logic.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="rewrite"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddMergedDocumentRewriter(
        this IRequestExecutorBuilder builder,
        Func<DocumentNode, DocumentNode> rewrite)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rewrite == null)
        {
            throw new ArgumentNullException(nameof(rewrite));
        }

        return builder.ConfigureSchema(s => s.AddMergedDocRewriter(rewrite));
    }

    /// <summary>
    /// Adds a schema visitor that is executed on
    /// the merged schema document.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="visit">
    /// A delegate that is called to execute the
    /// document visitation logic.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>, or
    /// <paramref name="visit"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddMergedDocVisitor(
        this IRequestExecutorBuilder builder,
        Action<DocumentNode> visit)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (visit == null)
        {
            throw new ArgumentNullException(nameof(visit));
        }

        return builder.ConfigureSchema(
            s => s.AddMergedDocVisitor(visit));
    }

    /// <summary>
    /// Removes the root types of remote schemas before merging them into the main schema.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="schemaName">
    /// The schema to which this rewriter applies to.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder IgnoreRootTypes(
        this IRequestExecutorBuilder builder,
        string? schemaName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return builder.AddDocumentRewriter(
            new RemoveRootTypeRewriter(schemaName));
    }

    /// <summary>
    /// Removes a file from the schema document that is being imported.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="typeName">
    /// The type that shall be removed from the schema document.
    /// </param>
    /// <param name="schemaName">
    /// The schema to which this rewriter applies to.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder IgnoreType(
        this IRequestExecutorBuilder builder,
        string typeName,
        string? schemaName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        typeName.EnsureGraphQLName(nameof(typeName));
        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return builder.AddDocumentRewriter(
            new RemoveTypeRewriter(typeName, schemaName));
    }

    public static IRequestExecutorBuilder IgnoreField(
        this IRequestExecutorBuilder builder,
        string typeName,
        string fieldName,
        string? schemaName = null) =>
        IgnoreField(builder, new FieldReference(typeName, fieldName), schemaName);

    public static IRequestExecutorBuilder IgnoreField(
        this IRequestExecutorBuilder builder,
        FieldReference field,
        string? schemaName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return builder.AddTypeRewriter(new RemoveFieldRewriter(field, schemaName));
    }

    public static IRequestExecutorBuilder RenameType(
        this IRequestExecutorBuilder builder,
        string originalTypeName,
        string newTypeName,
        string? schemaName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        originalTypeName.EnsureGraphQLName(nameof(originalTypeName));
        newTypeName.EnsureGraphQLName(nameof(newTypeName));
        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return builder.AddTypeRewriter(
            new RenameTypeRewriter(originalTypeName, newTypeName, schemaName));
    }

    public static IRequestExecutorBuilder RewriteType(
        this IRequestExecutorBuilder builder,
        string originalTypeName,
        string newTypeName,
        string schemaName)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        originalTypeName.EnsureGraphQLName(nameof(originalTypeName));
        newTypeName.EnsureGraphQLName(nameof(newTypeName));
        schemaName.EnsureGraphQLName(nameof(schemaName));

        return builder.ConfigureSchema(
            s => s.AddNameLookup(originalTypeName, newTypeName, schemaName));
    }

    public static IRequestExecutorBuilder RenameField(
        this IRequestExecutorBuilder builder,
        string typeName,
        string fieldName,
        string newFieldName,
        string? schemaName = null) =>
        RenameField(
            builder,
            new FieldReference(typeName, fieldName),
            newFieldName,
            schemaName);

    public static IRequestExecutorBuilder RenameField(
        this IRequestExecutorBuilder builder,
        FieldReference field,
        string newFieldName,
        string? schemaName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        schemaName?.EnsureGraphQLName(nameof(schemaName));
        newFieldName.EnsureGraphQLName(nameof(newFieldName));

        return builder.AddTypeRewriter(
            new RenameFieldRewriter(field, newFieldName, schemaName));
    }

    public static IRequestExecutorBuilder RenameField(
        this IRequestExecutorBuilder builder,
        string typeName,
        string fieldName,
        string argumentName,
        string newArgumentName,
        string? schemaName = null) =>
        RenameField(builder,
            new FieldReference(typeName, fieldName),
            argumentName,
            newArgumentName,
            schemaName);

    public static IRequestExecutorBuilder RenameField(
        this IRequestExecutorBuilder builder,
        FieldReference field,
        string argumentName,
        string newArgumentName,
        string? schemaName = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (field is null)
        {
            throw new ArgumentNullException(nameof(field));
        }

        argumentName.EnsureGraphQLName(nameof(argumentName));
        newArgumentName.EnsureGraphQLName(nameof(newArgumentName));
        schemaName?.EnsureGraphQLName(nameof(schemaName));

        return builder.AddTypeRewriter(
            new RenameFieldArgumentRewriter(
                field,
                argumentName,
                newArgumentName,
                schemaName));
    }

    public static IRequestExecutorBuilder AddTypeRewriter(
        this IRequestExecutorBuilder builder,
        RewriteTypeDefinitionDelegate rewrite)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rewrite is null)
        {
            throw new ArgumentNullException(nameof(rewrite));
        }

        return builder.AddTypeRewriter(new DelegateTypeRewriter(rewrite));
    }

    public static IRequestExecutorBuilder AddDocumentRewriter(
        this IRequestExecutorBuilder builder,
        RewriteDocumentDelegate rewrite)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (rewrite is null)
        {
            throw new ArgumentNullException(nameof(rewrite));
        }

        return builder.AddDocumentRewriter(
            new DelegateDocumentRewriter(rewrite));
    }

    public static IRequestExecutorBuilder AddTypeMergeHandler<T>(
        this IRequestExecutorBuilder builder)
        where T : class, ITypeMergeHandler
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddTypeMergeRule(
            SchemaMergerExtensions.CreateTypeMergeRule<T>());
    }

    public static IRequestExecutorBuilder AddDirectiveMergeHandler<T>(
        this IRequestExecutorBuilder builder)
        where T : class, IDirectiveMergeHandler
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddDirectiveMergeRule(
            SchemaMergerExtensions.CreateDirectiveMergeRule<T>());
    }

    private static string GetArgumentValue(DirectiveNode directive, string argumentName)
    {
        var argument = directive.Arguments
            .FirstOrDefault(a => a.Name.Value.EqualsOrdinal(argumentName));

        if (argument is null)
        {
            throw RequestExecutorBuilder_ArgumentWithNameWasNotFound(argumentName);
        }

        if (argument.Value is StringValueNode sv)
        {
            return sv.Value;
        }

        throw RequestExecutorBuilder_ArgumentValueWasNotAStringValue(argumentName);
    }
}
