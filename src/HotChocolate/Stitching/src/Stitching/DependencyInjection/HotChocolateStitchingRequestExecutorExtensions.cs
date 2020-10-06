using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Merge;
using HotChocolate.Stitching.Merge.Rewriters;
using HotChocolate.Stitching.Pipeline;
using HotChocolate.Stitching.Requests;
using HotChocolate.Utilities.Introspection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingRequestExecutorExtensions
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
                .UseInstrumentations()
                .UseExceptions()
                .UseDocumentCache()
                .UseReadPersistedQuery()
                .UseWritePersistedQuery()
                .UseDocumentParser()
                .UseDocumentValidation()
                .UseOperationCache()
                .UseOperationResolver()
                .UseOperationVariableCoercion()
                .UseHttpRequests();
        }

        public static IRequestExecutorBuilder AddRemoteSchema(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            bool ignoreRootTypes = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return AddRemoteSchema(
                builder,
                schemaName,
                async (services, cancellationToken) =>
                {
                    // The schema will be fetched via HTTP from the downstream service.
                    // We will use the schema name to get a the HttpClient, which
                    // we expect is correctly configured.
                    HttpClient httpClient = services
                        .GetRequiredService<IHttpClientFactory>()
                        .CreateClient(schemaName);

                    // The introspection client will do all the hard work to negotiate
                    // the features this schema supports and convert the introspection
                    // result into a parsed GraphQL SDL document.
                    return await new IntrospectionClient()
                        .DownloadSchemaAsync(httpClient, cancellationToken)
                        .ConfigureAwait(false);
                },
                ignoreRootTypes);
        }

        public static IRequestExecutorBuilder AddRemoteSchemaFromString(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            string schemaSdl,
            bool ignoreRootTypes = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return AddRemoteSchema(
                builder,
                schemaName,
                (services, cancellationToken) =>
                    new ValueTask<DocumentNode>(Utf8GraphQLParser.Parse(schemaSdl)),
                ignoreRootTypes);
        }

        public static IRequestExecutorBuilder AddRemoteSchemaFromFile(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            string fileName,
            bool ignoreRootTypes = false)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return AddRemoteSchema(
                builder,
                schemaName,
                async (services, cancellationToken) =>
                {
#if NETSTANDARD2_0
                    byte[] schemaSdl = await Task
                        .Run(() => File.ReadAllBytes(fileName), cancellationToken)
                        .ConfigureAwait(false);
#else
                    byte[] schemaSdl = await File
                        .ReadAllBytesAsync(fileName, cancellationToken)
                        .ConfigureAwait(false);
#endif

                    return Utf8GraphQLParser.Parse(schemaSdl);
                },
                ignoreRootTypes);
        }

        private static IRequestExecutorBuilder AddRemoteSchema(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            Func<IServiceProvider, CancellationToken, ValueTask<DocumentNode>> loadSchema,
            bool ignoreRootTypes)
        {
            // first we add a full GraphQL schema and executor that represents the remote schema.
            // This remote schema will be used by the stitching engine to execute queries against
            // this schema and also to lookup types in order correctly convert between scalars.
            builder
                .AddGraphQL(schemaName)
                .ConfigureSchemaAsync(
                    async (services, schemaBuilder, cancellationToken) =>
                    {
                        // No we need to load the schema document.
                        DocumentNode document =
                            await loadSchema(services, cancellationToken)
                                .ConfigureAwait(false);

                        // The document is used to create a SDL-first schema ...
                        schemaBuilder.AddDocument(document);

                        // ... which will fail if any resolver is actually used ...
                        // todo : how bind resolvers
                        schemaBuilder.Use(next => context => throw new NotSupportedException());
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
                    var autoProxy = await AutoUpdateRequestExecutorProxy.CreateAsync(
                        new RequestExecutorProxy(
                            services.GetRequiredService<IRequestExecutorResolver>(),
                            schemaName),
                        cancellationToken);

                    schemaBuilder
                        .AddRemoteExecutor(schemaName, autoProxy)
                        .TryAddSchemaInterceptor<StitchingSchemaInterceptor>()
                        .TryAddTypeInterceptor<StitchingTypeInterceptor>();
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

            return builder.ConfigureSchema(
                s => s.AddTypeRewriter(rewriter));
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
                    byte[] content = await File
                        .ReadAllBytesAsync(fileName, ct)
                        .ConfigureAwait(false);
#endif

                    s.AddTypeExtensions(Utf8GraphQLParser.Parse(content));
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

            return builder.ConfigureSchema(
                s => s.AddMergedDocRewriter(rewrite));
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
        /// <returns>
        /// Returns the <see cref="IRequestExecutorBuilder"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="builder"/> is <c>null</c>.
        /// </exception>
        public static IRequestExecutorBuilder IgnoreRootTypes(
            this IRequestExecutorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddDocumentRewriter(
                new RemoveRootTypeRewriter());
        }

        public static IRequestExecutorBuilder IgnoreType(
            this IRequestExecutorBuilder builder,
            NameString typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            typeName.EnsureNotEmpty(nameof(typeName));

            return builder.AddDocumentRewriter(
                new RemoveTypeRewriter(typeName));
        }

        public static IRequestExecutorBuilder IgnoreType(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            NameString typeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            typeName.EnsureNotEmpty(nameof(typeName));

            return builder.AddDocumentRewriter(
                new RemoveTypeRewriter(schemaName, typeName));
        }

        public static IRequestExecutorBuilder IgnoreField(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            NameString typeName,
            NameString fieldName) =>
            IgnoreField(builder, schemaName,
                new FieldReference(typeName, fieldName));

        public static IRequestExecutorBuilder IgnoreField(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            FieldReference field)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));

            return builder.AddTypeRewriter(
                new RemoveFieldRewriter(schemaName, field));
        }

        public static IRequestExecutorBuilder RenameType(
            this IRequestExecutorBuilder builder,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return builder.AddTypeRewriter(
                new RenameTypeRewriter(originalTypeName, newTypeName));
        }

        public static IRequestExecutorBuilder RenameType(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));

            return builder.AddTypeRewriter(
                new RenameTypeRewriter(
                    schemaName, originalTypeName, newTypeName));
        }

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            NameString newFieldName) =>
            RenameField(builder,
                new FieldReference(typeName, fieldName),
                newFieldName);

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            FieldReference field,
            NameString newFieldName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return builder.AddTypeRewriter(
                new RenameFieldRewriter(
                    field, newFieldName));
        }

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            NameString typeName,
            NameString fieldName,
            NameString newFieldName) =>
            RenameField(builder,
                schemaName,
                new FieldReference(typeName, fieldName),
                newFieldName);

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            FieldReference field,
            NameString newFieldName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            newFieldName.EnsureNotEmpty(nameof(newFieldName));

            return builder.AddTypeRewriter(
                new RenameFieldRewriter(
                    schemaName, field, newFieldName));
        }

        public static IRequestExecutorBuilder RenameFieldArgument(
            this IRequestExecutorBuilder builder,
            NameString typeName,
            NameString fieldName,
            NameString argumentName,
            NameString newArgumentName) =>
            RenameField(builder,
                new FieldReference(typeName, fieldName),
                argumentName,
                newArgumentName);

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            FieldReference field,
            NameString argumentName,
            NameString newArgumentName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            argumentName.EnsureNotEmpty(nameof(argumentName));
            newArgumentName.EnsureNotEmpty(nameof(newArgumentName));

            return builder.AddTypeRewriter(
                new RenameFieldArgumentRewriter(
                    field,
                    argumentName,
                    newArgumentName));
        }

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            NameString typeName,
            NameString fieldName,
            NameString argumentName,
            NameString newArgumentName) =>
            RenameField(builder,
                schemaName,
                new FieldReference(typeName, fieldName),
                argumentName,
                newArgumentName);

        public static IRequestExecutorBuilder RenameField(
            this IRequestExecutorBuilder builder,
            NameString schemaName,
            FieldReference field,
            NameString argumentName,
            NameString newArgumentName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            schemaName.EnsureNotEmpty(nameof(schemaName));
            argumentName.EnsureNotEmpty(nameof(argumentName));
            newArgumentName.EnsureNotEmpty(nameof(newArgumentName));

            return builder.AddTypeRewriter(
                new RenameFieldArgumentRewriter(
                    schemaName,
                    field,
                    argumentName,
                    newArgumentName));
        }

        public static IRequestExecutorBuilder AddTypeRewriter(
            this IRequestExecutorBuilder builder,
            RewriteTypeDefinitionDelegate rewrite)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rewrite == null)
            {
                throw new ArgumentNullException(nameof(rewrite));
            }

            return builder.AddTypeRewriter(new DelegateTypeRewriter(rewrite));
        }

        public static IRequestExecutorBuilder AddDocumentRewriter(
            this IRequestExecutorBuilder builder,
            RewriteDocumentDelegate rewrite)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (rewrite == null)
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
    }
}
