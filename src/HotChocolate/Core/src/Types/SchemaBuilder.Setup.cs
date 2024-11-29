#nullable enable

using HotChocolate.Configuration;
using HotChocolate.Configuration.Validation;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Factories;
using HotChocolate.Types.Helpers;
using HotChocolate.Types.Interceptors;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate;

public partial class SchemaBuilder
{
    private static class Setup
    {
        public static Schema Create(SchemaBuilder builder)
        {
            LazySchema schema = new();
            IDescriptorContext context = CreateContext(builder, schema);
            return Create(builder, schema, context);
        }

        public static Schema Create(
            SchemaBuilder builder,
            LazySchema lazySchema,
            IDescriptorContext context)
        {
            try
            {
                var typeInterceptors = new List<TypeInterceptor>();

                if (context.Options.StrictRuntimeTypeValidation &&
                    !builder._typeInterceptors.Contains(typeof(TypeValidationTypeInterceptor)))
                {
                    builder._typeInterceptors.Add(typeof(TypeValidationTypeInterceptor));
                }

                if (context.Options.EnableFlagEnums &&
                    !builder._typeInterceptors.Contains(typeof(FlagsEnumInterceptor)))
                {
                    builder._typeInterceptors.Add(typeof(FlagsEnumInterceptor));
                }

                if (context.Options.RemoveUnusedTypeSystemDirectives &&
                    !builder._typeInterceptors.Contains(typeof(DirectiveTypeInterceptor)))
                {
                    builder._typeInterceptors.Add(typeof(DirectiveTypeInterceptor));
                }

                if(builder._schemaFirstTypeInterceptor is not null)
                {
                    typeInterceptors.Add(builder._schemaFirstTypeInterceptor);
                }

                context.ContextData[typeof(PagingOptions).FullName!] = builder._pagingOptions;

                InitializeInterceptors(
                    context.Services,
                    builder._typeInterceptors,
                    typeInterceptors);

                ((AggregateTypeInterceptor)context.TypeInterceptor)
                    .SetInterceptors(typeInterceptors);

                context.TypeInterceptor.OnBeforeCreateSchemaInternal(context, builder);

                var typeReferences = CreateTypeReferences(builder, context);
                var typeRegistry = InitializeTypes(builder, context, typeReferences);

                return CompleteSchema(builder, context, lazySchema, typeRegistry);
            }
            catch (Exception ex)
            {
                context.TypeInterceptor.OnCreateSchemaError(context, ex);
                throw;
            }
            finally
            {
                TypeMemHelper.Clear();
            }
        }

        public static DescriptorContext CreateContext(
            SchemaBuilder builder,
            LazySchema lazySchema)
        {
            var services = builder._services ?? CreateDefaultServiceProvider(lazySchema);

            var typeInterceptor = new AggregateTypeInterceptor();

            var context = DescriptorContext.Create(
                () => builder._options,
                services,
                builder._conventions,
                builder._contextData,
                lazySchema,
                typeInterceptor);

            return context;
        }

        private static IServiceProvider CreateDefaultServiceProvider(LazySchema lazySchema)
        {
            var services = new ServiceCollection();
            AddCoreSchemaServices(services, lazySchema);
            return services.BuildServiceProvider();
        }

        private static List<TypeReference> CreateTypeReferences(
            SchemaBuilder builder,
            IDescriptorContext context)
        {
            var types = new List<TypeReference>();

            foreach (var typeRef in builder._types)
            {
                types.Add(typeRef(context.TypeInspector));
            }

            if (builder._documents.Count > 0)
            {
                types.AddRange(ParseDocuments(builder, context));
            }

            types.Add(builder._schema is null
                ? new SchemaTypeReference(new Schema())
                : builder._schema(context.TypeInspector));

            return types;
        }

        private static IEnumerable<TypeReference> ParseDocuments(
            SchemaBuilder builder,
            IDescriptorContext context)
        {
            var types = new List<TypeReference>();
            var documents = new List<DocumentNode>();
            context.ContextData[WellKnownContextData.SchemaDocuments] = documents;

            foreach (var fetchSchema in builder._documents)
            {
                var schemaDocument = fetchSchema(context.Services);
                schemaDocument = schemaDocument.RemoveBuiltInTypes();
                documents.Add(schemaDocument);

                var visitorContext = new SchemaSyntaxVisitorContext(
                    context,
                    builder._schemaFirstTypeInterceptor!.Directives);
                var visitor = new SchemaSyntaxVisitor();

                visitor.Visit(schemaDocument, visitorContext);
                types.AddRange(visitorContext.Types);

                RegisterOperationName(
                    builder,
                    OperationType.Query,
                    visitorContext.QueryTypeName);

                RegisterOperationName(
                    builder,
                    OperationType.Mutation,
                    visitorContext.MutationTypeName);

                RegisterOperationName(
                    builder,
                    OperationType.Subscription,
                    visitorContext.SubscriptionTypeName);

                var directives =
                    visitorContext.Directives ?? Array.Empty<DirectiveNode>();

                if (builder._schema is null && (directives.Count > 0 || visitorContext.Description != null))
                {
                    builder.SetSchema(new Schema(d =>
                    {
                        d.Description(visitorContext.Description);
                        foreach (var directive in directives)
                        {
                            d.Directive(directive);
                        }
                    }));
                }
            }

            return types;
        }

        private static void RegisterOperationName(
            SchemaBuilder builder,
            OperationType operation,
            string? typeName)
        {
            if (!builder._operations.ContainsKey(operation) && !string.IsNullOrEmpty(typeName))
            {
                builder._operations.Add(
                    operation,
                    _ => TypeReference.Create(typeName, TypeContext.Output));
            }
        }

        private static TypeRegistry InitializeTypes(
            SchemaBuilder builder,
            IDescriptorContext context,
            IReadOnlyList<TypeReference> types)
        {
            var typeRegistry = new TypeRegistry(context.TypeInterceptor);
            var initializer =
                CreateTypeInitializer(builder, context, types, typeRegistry);
            initializer.Initialize();
            return typeRegistry;
        }

        private static TypeInitializer CreateTypeInitializer(
            SchemaBuilder builder,
            IDescriptorContext context,
            IReadOnlyList<TypeReference> typeReferences,
            TypeRegistry typeRegistry)
        {
            var operations =
                builder._operations.ToDictionary(
                    t => t.Key,
                    t => t.Value(context.TypeInspector));

            var initializer = new TypeInitializer(
                context,
                typeRegistry,
                typeReferences,
                builder._isOfType,
                type => GetOperationKind(type, context.TypeInspector, operations),
                builder._options);

            foreach (var component in builder._globalComponents)
            {
                initializer.GlobalComponents.Add(component);
            }

            foreach (var binding in builder._clrTypes)
            {
                typeRegistry.TryRegister(
                    (ExtendedTypeReference)binding.Value.Item1(context.TypeInspector),
                    binding.Value.Item2.Invoke(context.TypeInspector));
            }

            return initializer;
        }

        private static void InitializeInterceptors<T>(
            IServiceProvider services,
            IReadOnlyList<object> registered,
            List<T> interceptors)
            where T : class
        {
            if (services is not EmptyServiceProvider &&
                services.GetService<IEnumerable<T>>() is { } fromService)
            {
                interceptors.AddRange(fromService);
            }

            if (registered.Count > 0)
            {
                foreach (var interceptorOrType in registered)
                {
                    if (interceptorOrType is Type type)
                    {
                        var obj = ActivatorUtilities.CreateInstance(services, type);
                        if (obj is T casted)
                        {
                            interceptors.Add(casted);
                        }
                    }
                    else if (interceptorOrType is T interceptor)
                    {
                        interceptors.Add(interceptor);
                    }
                }
            }
        }

        private static RootTypeKind GetOperationKind(
            TypeSystemObjectBase type,
            ITypeInspector typeInspector,
            Dictionary<OperationType, TypeReference> operations)
        {
            if (type is ObjectType objectType)
            {
                if (IsOperationType(
                        objectType,
                        OperationType.Query,
                        typeInspector,
                        operations))
                {
                    return RootTypeKind.Query;
                }

                if (IsOperationType(
                        objectType,
                        OperationType.Mutation,
                        typeInspector,
                        operations))
                {
                    return RootTypeKind.Mutation;
                }

                if (IsOperationType(
                        objectType,
                        OperationType.Subscription,
                        typeInspector,
                        operations))
                {
                    return RootTypeKind.Subscription;
                }
            }

            return RootTypeKind.None;
        }

        private static bool IsOperationType(
            ObjectType objectType,
            OperationType operationType,
            ITypeInspector typeInspector,
            Dictionary<OperationType, TypeReference> operations)
        {
            if (operations.TryGetValue(operationType, out var typeRef))
            {
                if (typeRef is SchemaTypeReference sr)
                {
                    return sr.Type == objectType;
                }

                if (typeRef is ExtendedTypeReference cr)
                {
                    return cr.Type.Equals(typeInspector.GetType(objectType.GetType())) ||
                        cr.Type.Equals(typeInspector.GetType(objectType.RuntimeType));
                }

                if (typeRef is SyntaxTypeReference str)
                {
                    return objectType.Name.EqualsOrdinal(str.Type.NamedType().Name.Value);
                }
            }
            else if (operationType == OperationType.Query)
            {
                return objectType.Name.EqualsOrdinal(OperationTypeNames.Query);
            }
            else if (operationType == OperationType.Mutation)
            {
                return objectType.Name.EqualsOrdinal(OperationTypeNames.Mutation);
            }
            else if (operationType == OperationType.Subscription)
            {
                return objectType.Name.EqualsOrdinal(OperationTypeNames.Subscription);
            }

            return false;
        }

        private static Schema CompleteSchema(
            SchemaBuilder builder,
            IDescriptorContext context,
            LazySchema lazySchema,
            TypeRegistry typeRegistry)
        {
            var definition = CreateSchemaDefinition(builder, context, typeRegistry);
            context.TypeInterceptor.OnBeforeRegisterSchemaTypes(context, definition);

            var schema = typeRegistry.Types.Select(t => t.Type).OfType<Schema>().First();
            schema.CompleteSchema(definition);

            if (SchemaValidator.Validate(context, schema) is { Count: > 0 } errors)
            {
                throw new SchemaException(errors);
            }

            if (definition.QueryType is null && builder._options.StrictValidation)
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.SchemaBuilder_NoQueryType)
                        .Build());
            }

            context.TypeInterceptor.OnAfterCreateSchemaInternal(context, schema);
            lazySchema.Schema = schema;

            return schema;
        }

        private static SchemaTypesDefinition CreateSchemaDefinition(
            SchemaBuilder builder,
            IDescriptorContext context,
            TypeRegistry typeRegistry)
        {
            var definition = new SchemaTypesDefinition();

            RegisterOperationName(
                builder,
                OperationType.Query,
                builder._options.QueryTypeName);

            RegisterOperationName(
                builder,
                OperationType.Mutation,
                builder._options.MutationTypeName);

            RegisterOperationName(
                builder,
                OperationType.Subscription,
                builder._options.SubscriptionTypeName);

            var operations = builder._operations.ToDictionary(
                static t => t.Key,
                t => t.Value(context.TypeInspector));

            ResolveOperations(definition, operations, typeRegistry);

            var types = RemoveUnreachableTypes(builder, typeRegistry, definition);

            definition.Types = types.OfType<INamedType>().Distinct().ToArray();
            definition.DirectiveTypes = types.OfType<DirectiveType>().Distinct().ToArray();

            return definition;
        }

        private static void ResolveOperations(
            SchemaTypesDefinition schemaDef,
            Dictionary<OperationType, TypeReference> operations,
            TypeRegistry typeRegistry)
        {
            if (operations.Count == 0)
            {
                schemaDef.QueryType = GetObjectType(OperationTypeNames.Query);
                schemaDef.MutationType = GetObjectType(OperationTypeNames.Mutation);
                schemaDef.SubscriptionType = GetObjectType(OperationTypeNames.Subscription);
            }
            else
            {
                schemaDef.QueryType = GetOperationType(OperationType.Query);
                schemaDef.MutationType = GetOperationType(OperationType.Mutation);
                schemaDef.SubscriptionType = GetOperationType(OperationType.Subscription);
            }

            return;

            ObjectType? GetObjectType(string typeName)
            {
                foreach (var registeredType in typeRegistry.Types)
                {
                    if (registeredType.Type is ObjectType objectType &&
                        objectType.Name.EqualsOrdinal(typeName))
                    {
                        return objectType;
                    }
                }

                return null;
            }

            ObjectType? GetOperationType(OperationType operation)
            {
                if (operations.TryGetValue(operation, out var reference))
                {
                    if (reference is SchemaTypeReference sr)
                    {
                        return (ObjectType)sr.Type;
                    }

                    if (reference is ExtendedTypeReference cr &&
                        typeRegistry.TryGetType(cr, out var registeredType))
                    {
                        return (ObjectType)registeredType.Type;
                    }

                    if (reference is SyntaxTypeReference str)
                    {
                        var namedType = str.Type.NamedType();
                        return typeRegistry.Types
                            .Select(t => t.Type)
                            .OfType<ObjectType>()
                            .FirstOrDefault(t => t.Name.EqualsOrdinal(namedType.Name.Value));
                    }
                }

                return null;
            }
        }

        private static IReadOnlyCollection<TypeSystemObjectBase> RemoveUnreachableTypes(
            SchemaBuilder builder,
            TypeRegistry typeRegistry,
            SchemaTypesDefinition definition)
        {
            if (builder._options.RemoveUnreachableTypes)
            {
                var trimmer = new TypeTrimmer(typeRegistry.Types.Select(t => t.Type));
                trimmer.AddOperationType(definition.QueryType);
                trimmer.AddOperationType(definition.MutationType);
                trimmer.AddOperationType(definition.SubscriptionType);
                return trimmer.Trim();
            }

            return typeRegistry.Types.Select(t => t.Type).ToArray();
        }
    }

    internal static void AddCoreSchemaServices(IServiceCollection services, LazySchema lazySchema)
    {
        services.TryAddSingleton(lazySchema);
        services.TryAddSingleton(static sp => sp.GetRequiredService<LazySchema>().Schema);

        // If there was now node id serializer registered we will register the default one as a fallback.
        services.TryAddSingleton<INodeIdSerializer>(static sp =>
        {
            var appServices = sp.GetService<IApplicationServiceProvider>();
            INodeIdValueSerializer[]? allSerializers = null;

            if (appServices is not null)
            {
                allSerializers = sp.GetRequiredService<IApplicationServiceProvider>()
                    .GetServices<INodeIdValueSerializer>()
                    .ToArray();
            }

            if(allSerializers is null || allSerializers.Length == 0)
            {
                allSerializers =
                [
                    new StringNodeIdValueSerializer(),
                    new Int16NodeIdValueSerializer(),
                    new Int32NodeIdValueSerializer(),
                    new Int64NodeIdValueSerializer(),
                    new GuidNodeIdValueSerializer()
                ];
            }

            return new DefaultNodeIdSerializer(allSerializers, 1024);
        });

        services.TryAddSingleton<INodeIdSerializerAccessor>(
            static sp =>
            {
                var lazy = sp.GetRequiredService<LazySchema>();
                var accessor = new NodeIdSerializerAccessor();
                lazy.OnSchemaCreated(accessor.OnSchemaCreated);
                return accessor;
            });
    }
}
