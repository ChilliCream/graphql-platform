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
                var lazySchema = new LazySchema();
                DescriptorContext context = CreateContext(builder, lazySchema);

                try
                {
                    IBindingLookup bindingLookup = builder._bindingCompiler.Compile(context);

                    IReadOnlyList<ITypeReference> typeReferences =
                        CreateTypeReferences(builder, context, bindingLookup);

                    TypeRegistry typeRegistry =
                        InitializeTypes(builder, context, bindingLookup, typeReferences,
                            lazySchema);

                    return CompleteSchema(builder, context, lazySchema, typeRegistry);
                }
                catch (Exception ex)
                {
                    context.SchemaInterceptor.OnError(context, ex);
                    throw;
                }
            }

            private static DescriptorContext CreateContext(
                SchemaBuilder builder,
                LazySchema lazySchema)
            {
                IServiceProvider services = builder._services ?? new EmptyServiceProvider();
                var schemaInterceptors = new List<ISchemaInterceptor>();
                var typeInterceptors = new List<ITypeInitializationInterceptor>();

                InitializeInterceptors(services, builder._schemaInterceptors, schemaInterceptors);
                InitializeInterceptors(services, builder._typeInterceptors, typeInterceptors);

                var schemaInterceptor = new AggregateSchemaInterceptor(schemaInterceptors);
                var typeInterceptor = new AggregateTypeInterceptor(typeInterceptors);

                DescriptorContext context = DescriptorContext.Create(
                    builder._options,
                    services,
                    builder._conventions,
                    builder._contextData,
                    lazySchema,
                    schemaInterceptor,
                    typeInterceptor);

                schemaInterceptor.OnBeforeCreate(context, builder);

                return context;
            }

            private static IReadOnlyList<ITypeReference> CreateTypeReferences(
                SchemaBuilder builder,
                DescriptorContext context,
                IBindingLookup bindingLookup)
            {
                var types = new List<ITypeReference>();

                foreach (CreateRef typeRef in builder._types)
                {
                    types.Add(typeRef(context.TypeInspector));
                }

                if (builder._documents.Count > 0)
                {
                    types.AddRange(ParseDocuments(builder, context, bindingLookup));
                }

                if (builder._schema is null)
                {
                    types.Add(new SchemaTypeReference(new Schema()));
                }
                else
                {
                    types.Add(builder._schema(context.TypeInspector));
                }

                return types;
            }

            private static IEnumerable<ITypeReference> ParseDocuments(
                SchemaBuilder builder,
                DescriptorContext context,
                IBindingLookup bindingLookup)
            {
                var types = new List<ITypeReference>();

                foreach (LoadSchemaDocument fetchSchema in builder._documents)
                {
                    DocumentNode schemaDocument = fetchSchema(context.Services);
                    schemaDocument = schemaDocument.RemoveBuiltInTypes();

                    var visitor = new SchemaSyntaxVisitor(bindingLookup);
                    visitor.Visit(schemaDocument, null);
                    types.AddRange(visitor.Types);

                    RegisterOperationName(
                        builder,
                        OperationType.Query,
                        visitor.QueryTypeName);

                    RegisterOperationName(
                        builder,
                        OperationType.Mutation,
                        visitor.MutationTypeName);

                    RegisterOperationName(
                        builder,
                        OperationType.Subscription,
                        visitor.SubscriptionTypeName);

                    IReadOnlyCollection<DirectiveNode> directives =
                        visitor.Directives ?? Array.Empty<DirectiveNode>();

                    if (builder._schema is null
                        && (directives.Count > 0
                        || visitor.Description != null))
                    {
                        builder.SetSchema(new Schema(d =>
                        {
                            d.Description(visitor.Description);
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
                string typeName)
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
                DescriptorContext context,
                IBindingLookup bindingLookup,
                IReadOnlyList<ITypeReference> types,
                LazySchema lazySchema)
            {
                var typeRegistry = new TypeRegistry(context.TypeInterceptor);
                TypeInitializer initializer =
                    CreateTypeInitializer(builder, context, bindingLookup, types, typeRegistry);
                initializer.Initialize(() => lazySchema.Schema, builder._options);
                return typeRegistry;
            }

            private static TypeInitializer CreateTypeInitializer(
                SchemaBuilder builder,
                IDescriptorContext context,
                IBindingLookup bindingLookup,
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
                    builder._resolverTypes,
                    builder._isOfType,
                    type => IsQueryType(context.TypeInspector, type, operations),
                    type => IsMutationType(context.TypeInspector, type, operations));

                foreach (FieldMiddleware component in builder._globalComponents)
                {
                    initializer.GlobalComponents.Add(component);
                }

                foreach (FieldReference reference in builder._resolvers.Keys)
                {
                    initializer.Resolvers[reference] = new RegisteredResolver(
                        typeof(object), builder._resolvers[reference]);
                }

                foreach (RegisteredResolver resolver in
                    bindingLookup.Bindings.SelectMany(t => t.CreateResolvers()))
                {
                    var reference = new FieldReference(
                        resolver.Field.TypeName,
                        resolver.Field.FieldName);
                    initializer.Resolvers[reference] = resolver;
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
                            object? obj = serviceFactory.CreateInstance(type);
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

            private static bool IsQueryType(
                ITypeInspector typeInspector,
                TypeSystemObjectBase type,
                Dictionary<OperationType, ITypeReference> operations)
            {
                if (type is ObjectType objectType)
                {
                    if (operations.TryGetValue(OperationType.Query, out ITypeReference? typeRef))
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
                    else
                    {
                        return type.Name.Equals(WellKnownTypes.Query);
                    }
                }

                return false;
            }

            private static bool IsMutationType(
                ITypeInspector typeInspector,
                TypeSystemObjectBase type,
                Dictionary<OperationType, ITypeReference> operations)
            {
                if (type is ObjectType objectType)
                {
                    if (operations.TryGetValue(OperationType.Mutation, out ITypeReference? typeRef))
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
                    else
                    {
                        return type.Name.Equals(WellKnownTypes.Mutation);
                    }
                }

                return false;
            }

            private static Schema CompleteSchema(
                SchemaBuilder builder,
                DescriptorContext context,
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
                DescriptorContext context,
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
