using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Properties;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Factories;
using HotChocolate.Utilities;
using HotChocolate.Utilities.Introspection;
using Microsoft.Extensions.DependencyInjection;

#nullable enable

namespace HotChocolate
{
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
                    var schemaInterceptors = new List<ISchemaInterceptor>();
                    var typeInterceptors = new List<ITypeInitializationInterceptor>();

                    InitializeInterceptors(
                        context.Services,
                        builder._schemaInterceptors,
                        schemaInterceptors);

                    InitializeInterceptors(
                        context.Services,
                        builder._typeInterceptors,
                        typeInterceptors);

                    ((AggregateSchemaInterceptor)context.SchemaInterceptor)
                        .SetInterceptors(schemaInterceptors);

                    ((AggregateTypeInterceptor)context.TypeInterceptor)
                        .SetInterceptors(typeInterceptors);

                    context.SchemaInterceptor.OnBeforeCreate(context, builder);

                    IReadOnlyList<ITypeReference> typeReferences =
                        CreateTypeReferences(builder, context);

                    TypeRegistry typeRegistry =
                        InitializeTypes(
                            builder,
                            context,
                            typeReferences,
                            lazySchema);

                    return CompleteSchema(builder, context, lazySchema, typeRegistry);
                }
                catch (Exception ex)
                {
                    context.SchemaInterceptor.OnError(context, ex);
                    throw;
                }
            }

            public static DescriptorContext CreateContext(
                SchemaBuilder builder,
                LazySchema lazySchema)
            {
                IServiceProvider services = builder._services ?? new EmptyServiceProvider();

                var schemaInterceptor = new AggregateSchemaInterceptor();
                var typeInterceptor = new AggregateTypeInterceptor();

                DescriptorContext context = DescriptorContext.Create(
                    builder._options,
                    services,
                    builder._conventions,
                    builder._contextData,
                    lazySchema,
                    schemaInterceptor,
                    typeInterceptor);

                return context;
            }

            private static IReadOnlyList<ITypeReference> CreateTypeReferences(
                SchemaBuilder builder,
                IDescriptorContext context)
            {
                var types = new List<ITypeReference>();

                foreach (CreateRef typeRef in builder._types)
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

            private static IEnumerable<ITypeReference> ParseDocuments(
                SchemaBuilder builder,
                IDescriptorContext context)
            {
                var types = new List<ITypeReference>();

                foreach (LoadSchemaDocument fetchSchema in builder._documents)
                {
                    DocumentNode schemaDocument = fetchSchema(context.Services);
                    schemaDocument = schemaDocument.RemoveBuiltInTypes();

                    var visitorContext = new SchemaSyntaxVisitorContext(context);
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

                    IReadOnlyCollection<DirectiveNode> directives =
                        visitorContext.Directives ?? Array.Empty<DirectiveNode>();

                    if (builder._schema is null
                        && (directives.Count > 0
                        || visitorContext.Description != null))
                    {
                        builder.SetSchema(new Schema(d =>
                        {
                            d.Description(visitorContext.Description);
                            foreach (DirectiveNode directive in directives)
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
                if (!builder._operations.ContainsKey(operation)
                    && !string.IsNullOrEmpty(typeName))
                {
                    builder._operations.Add(
                        operation,
                        _ => TypeReference.Create(typeName, TypeContext.Output));
                }
            }

            private static TypeRegistry InitializeTypes(
                SchemaBuilder builder,
                IDescriptorContext context,
                IReadOnlyList<ITypeReference> types,
                LazySchema lazySchema)
            {
                var typeRegistry = new TypeRegistry(context.TypeInterceptor);
                TypeInitializer initializer =
                    CreateTypeInitializer(builder, context, types, typeRegistry);
                initializer.Initialize(() => lazySchema.Schema, builder._options);
                return typeRegistry;
            }

            private static TypeInitializer CreateTypeInitializer(
                SchemaBuilder builder,
                IDescriptorContext context,
                IReadOnlyList<ITypeReference> typeReferences,
                TypeRegistry typeRegistry)
            {
                Dictionary<OperationType, ITypeReference> operations =
                    builder._operations.ToDictionary(
                        t => t.Key,
                        t => t.Value(context.TypeInspector));

                var initializer = new TypeInitializer(
                    context,
                    typeRegistry,
                    typeReferences,
                    builder._isOfType,
                    type => GetOperationKind(type, context.TypeInspector, operations));

                foreach (FieldMiddleware component in builder._globalComponents)
                {
                    initializer.GlobalComponents.Add(component);
                }

                foreach (KeyValuePair<Type, (CreateRef, CreateRef)> binding in builder._clrTypes)
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
                    var serviceFactory = new ServiceFactory { Services = services };

                    foreach (object interceptorOrType in registered)
                    {
                        if (interceptorOrType is Type type)
                        {
                            var obj = serviceFactory.CreateInstance(type);
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
                Dictionary<OperationType, ITypeReference> operations)
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
                Dictionary<OperationType, ITypeReference> operations)
            {
                if (operations.TryGetValue(operationType, out ITypeReference? typeRef))
                {
                    if (typeRef is SchemaTypeReference sr)
                    {
                        return sr.Type == objectType;
                    }

                    if (typeRef is ExtendedTypeReference cr)
                    {
                        return cr.Type == typeInspector.GetType(objectType.GetType())
                            || cr.Type == typeInspector.GetType(objectType.RuntimeType);
                    }

                    if (typeRef is SyntaxTypeReference str)
                    {
                        return objectType.Name.Equals(str.Type.NamedType().Name.Value);
                    }
                }
                else if(operationType == OperationType.Query)
                {
                    return objectType.Name.Equals(OperationTypeNames.Query);
                }
                else if(operationType == OperationType.Mutation)
                {
                    return objectType.Name.Equals(OperationTypeNames.Mutation);
                }
                else if(operationType == OperationType.Subscription)
                {
                    return objectType.Name.Equals(OperationTypeNames.Subscription);
                }

                return false;
            }

            private static Schema CompleteSchema(
                SchemaBuilder builder,
                IDescriptorContext context,
                LazySchema lazySchema,
                TypeRegistry typeRegistry)
            {
                SchemaTypesDefinition definition =
                    CreateSchemaDefinition(builder, context, typeRegistry);

                if (definition.QueryType is null && builder._options.StrictValidation)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(TypeResources.SchemaBuilder_NoQueryType)
                            .Build());
                }

                Schema schema = typeRegistry.Types
                    .Select(t => t.Type).OfType<Schema>().First();

                schema.CompleteSchema(definition);
                lazySchema.Schema = schema;
                context.SchemaInterceptor.OnAfterCreate(context, schema);
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

                Dictionary<OperationType, ITypeReference> operations =
                    builder._operations.ToDictionary(
                        t => t.Key,
                        t => t.Value(context.TypeInspector));

                definition.QueryType = ResolveOperation(
                    OperationType.Query, operations, typeRegistry);
                definition.MutationType = ResolveOperation(
                    OperationType.Mutation, operations, typeRegistry);
                definition.SubscriptionType = ResolveOperation(
                    OperationType.Subscription, operations, typeRegistry);

                IReadOnlyCollection<TypeSystemObjectBase> types =
                    RemoveUnreachableTypes(builder, typeRegistry, definition);

                definition.Types = types.OfType<INamedType>().Distinct().ToArray();
                definition.DirectiveTypes = types.OfType<DirectiveType>().Distinct().ToArray();

                return definition;
            }

            private static ObjectType? ResolveOperation(
                OperationType operation,
                Dictionary<OperationType, ITypeReference> operations,
                TypeRegistry typeRegistry)
            {
                if (!operations.ContainsKey(operation))
                {
                    NameString typeName = operation.ToString();
                    return typeRegistry.Types
                        .Select(t => t.Type)
                        .OfType<ObjectType>()
                        .FirstOrDefault(t => t.Name.Equals(typeName));
                }
                else if (operations.TryGetValue(operation, out ITypeReference? reference))
                {
                    if (reference is SchemaTypeReference sr)
                    {
                        return (ObjectType)sr.Type;
                    }

                    if (reference is ExtendedTypeReference cr &&
                        typeRegistry.TryGetType(cr, out RegisteredType? registeredType))
                    {
                        return (ObjectType)registeredType.Type;
                    }

                    if (reference is SyntaxTypeReference str)
                    {
                        NamedTypeNode namedType = str.Type.NamedType();
                        return typeRegistry.Types
                            .Select(t => t.Type)
                            .OfType<ObjectType>()
                            .FirstOrDefault(t => t.Name.Equals(
                                namedType.Name.Value));
                    }
                }

                return null;
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

                return typeRegistry.Types.Select(t => t.Type).ToList();
            }
        }
    }
}
