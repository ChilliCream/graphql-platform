#nullable enable

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Configuration;
using HotChocolate.Configuration.Validation;
using HotChocolate.Execution;
using HotChocolate.Features;
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

                if (context.Options.StrictRuntimeTypeValidation
                    && !builder._typeInterceptors.Contains(typeof(TypeValidationTypeInterceptor)))
                {
                    builder._typeInterceptors.Add(typeof(TypeValidationTypeInterceptor));
                }

                if (context.Options.EnableFlagEnums
                    && !builder._typeInterceptors.Contains(typeof(FlagsEnumInterceptor)))
                {
                    builder._typeInterceptors.Add(typeof(FlagsEnumInterceptor));
                }

                if (context.Options.RemoveUnusedTypeSystemDirectives
                    && !builder._typeInterceptors.Contains(typeof(DirectiveTypeInterceptor)))
                {
                    builder._typeInterceptors.Add(typeof(DirectiveTypeInterceptor));
                }

                if (builder.Features.Get<TypeSystemFeature>()?.SchemaDocuments.Count > 0)
                {
                    typeInterceptors.Add(new SchemaFirstTypeInterceptor());
                }

                var pagingOptions = builder.Features.GetOrSet<PagingOptions>();
                PagingDefaults.Apply(pagingOptions);

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

        public static DescriptorContext CreateContext(SchemaBuilder builder, LazySchema lazySchema)
        {
            var services = builder._services ?? CreateDefaultServiceProvider(lazySchema);
            var typeInterceptor = new AggregateTypeInterceptor();

            var context = DescriptorContext.Create(
                () => builder._options,
                services,
                builder.Features,
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

            types.AddRange(ParseDocuments(builder, context));

            types.Add(builder._schema is null
                ? new SchemaTypeReference(new Schema())
                : builder._schema(context.TypeInspector));

            return types;
        }

        private static IEnumerable<TypeReference> ParseDocuments(
            SchemaBuilder builder,
            IDescriptorContext context)
        {
            if (!builder.Features.TryGet(out TypeSystemFeature? feature)
                || feature.SchemaDocuments.Count == 0)
            {
                return [];
            }

            var types = new List<TypeReference>();
            var visitor = new SchemaSyntaxVisitor();

            foreach (var documentInfo in feature.SchemaDocuments)
            {
                var schemaDocument = documentInfo.Load(context.Services);
                schemaDocument = schemaDocument.RemoveBuiltInTypes();

                var visitorContext = new SchemaSyntaxVisitorContext(context)
                {
                    ScalarDirectives = feature.ScalarDirectives
                };

                visitor.Visit(schemaDocument, visitorContext);
                types.AddRange(visitorContext.Types);
                feature.ScalarDirectives = visitorContext.ScalarDirectives;

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

                var directives = visitorContext.Directives ?? [];

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

            if(builder.Features.Get<TypeSystemFeature>()?.RuntimeTypeBindings is { Count: > 0 } bindings)
            {
                foreach (var binding in bindings.Values)
                {
                    typeRegistry.TryRegister(
                        binding.GetRuntimeTypeReference(context.TypeInspector),
                        binding.GetSchemaTypeReference(context.TypeInspector));
                }
            }

            return initializer;
        }

        private static void InitializeInterceptors<T>(
            IServiceProvider services,
            IReadOnlyList<object> registered,
            List<T> interceptors)
            where T : class
        {
            if (services is not EmptyServiceProvider
                && services.GetService<IEnumerable<T>>() is { } fromService)
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
            TypeSystemObject type,
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
                    return cr.Type.Equals(typeInspector.GetType(objectType.GetType()))
                        || cr.Type.Equals(typeInspector.GetType(objectType.RuntimeType));
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
            var definition = CreateSchemaConfiguration(builder, context, typeRegistry);
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

        private static SchemaTypesConfiguration CreateSchemaConfiguration(
            SchemaBuilder builder,
            IDescriptorContext context,
            TypeRegistry typeRegistry)
        {
            var definition = new SchemaTypesConfiguration();

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

            var included = new HashSet<ITypeSystemMember>(typeRegistry.Types.Count);
            var types = new List<ITypeSystemMember>(typeRegistry.Types.Count);

            foreach (var registration in typeRegistry.Types)
            {
                switch (registration.Type)
                {
                    case ITypeDefinition typeDef when typeDef is not ITypeDefinitionExtension && included.Add(typeDef):
                        types.Add(typeDef);
                        break;

                    case DirectiveType directiveType when included.Add(directiveType):
                        types.Add(directiveType);
                        break;

                    default:
                        continue;
                }
            }

            RemoveUnreachableTypes(builder, definition, types);

            definition.Types = [.. types.OfType<ITypeDefinition>()];
            definition.DirectiveTypes = [.. types.OfType<DirectiveType>()];

            return definition;
        }

        private static void ResolveOperations(
            SchemaTypesConfiguration schemaDef,
            Dictionary<OperationType, TypeReference> operations,
            TypeRegistry typeRegistry)
        {
            if (operations.Count == 0)
            {
                schemaDef.QueryType = GetObjectType(OperationTypeNames.Query, OperationType.Query);
                schemaDef.MutationType = GetObjectType(OperationTypeNames.Mutation, OperationType.Mutation);
                schemaDef.SubscriptionType = GetObjectType(OperationTypeNames.Subscription, OperationType.Subscription);
            }
            else
            {
                schemaDef.QueryType = GetOperationType(OperationType.Query);
                schemaDef.MutationType = GetOperationType(OperationType.Mutation);
                schemaDef.SubscriptionType = GetOperationType(OperationType.Subscription);
            }
            return;

            ObjectType? GetObjectType(string typeName, OperationType expectedOperation)
            {
                foreach (var registeredType in typeRegistry.Types)
                {
                    if (registeredType.Type.Name.EqualsOrdinal(typeName))
                    {
                        if (registeredType.Type is not ObjectType objectType)
                        {
                            Throw((ITypeDefinition)registeredType.Type, expectedOperation);
                        }

                        return objectType;
                    }
                }

                return null;
            }

            ObjectType? GetOperationType(OperationType operation)
            {
                if (!operations.TryGetValue(operation, out var reference))
                {
                    return null;
                }

                switch (reference)
                {
                    case SchemaTypeReference str:
                        {
                            if (str.Type is not ObjectType ot)
                            {
                                Throw((ITypeDefinition)str.Type, operation);
                            }

                            return ot;
                        }

                    case ExtendedTypeReference cr when typeRegistry.TryGetType(cr, out var registeredType):
                        {
                            if (registeredType.Type is not ObjectType ot)
                            {
                                Throw((ITypeDefinition)registeredType.Type, operation);
                            }

                            return ot;
                        }

                    case SyntaxTypeReference str:
                        {
                            var namedType = str.Type.NamedType();
                            var type = typeRegistry.Types
                                .Select(t => t.Type)
                                .FirstOrDefault(t => t.Name.EqualsOrdinal(namedType.Name.Value));

                            if (type is null)
                            {
                                return null;
                            }

                            if (type is not ObjectType ot)
                            {
                                Throw((ITypeDefinition)type, operation);
                            }

                            return ot;
                        }

                    default:
                        return null;
                }
            }

            [DoesNotReturn]
            static void Throw(ITypeDefinition namedType, OperationType operation)
            {
                throw SchemaErrorBuilder.New()
                    .SetMessage(
                        "Cannot register `{0}` as {1} type as it is not an object type. `{0}` is of type `{2}`.",
                        namedType.Name,
                        operation,
                        namedType.GetType().FullName)
                    .SetTypeSystemObject((TypeSystemObject)namedType)
                    .BuildException();
            }
        }

        private static void RemoveUnreachableTypes(
            SchemaBuilder builder,
            SchemaTypesConfiguration configuration,
            List<ITypeSystemMember> types)
        {
            if (builder._options.RemoveUnreachableTypes)
            {
                var trimmer = new TypeTrimmer(types);
                trimmer.AddOperationType(configuration.QueryType);
                trimmer.AddOperationType(configuration.MutationType);
                trimmer.AddOperationType(configuration.SubscriptionType);
                trimmer.Trim();
            }
        }
    }

    internal static void AddCoreSchemaServices(IServiceCollection services, LazySchema lazySchema)
    {
        services.TryAddSingleton(lazySchema);
        services.TryAddSingleton(static sp => sp.GetRequiredService<LazySchema>().Schema);
        services.TryAddSingleton<ISchemaDefinition>(sp => sp.GetRequiredService<LazySchema>().Schema);

        // If there was now node id serializer registered, we will register the default one as a fallback.
        services.TryAddSingleton<INodeIdSerializer>(static sp =>
        {
            var appServices = sp.GetService<IRootServiceProviderAccessor>()?.ServiceProvider;
            INodeIdValueSerializer[]? allSerializers = null;

            if (appServices is not null)
            {
                allSerializers = [.. appServices.GetServices<INodeIdValueSerializer>()];
            }

            if (allSerializers is null || allSerializers.Length == 0)
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

            return new DefaultNodeIdSerializer(allSerializers);
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
