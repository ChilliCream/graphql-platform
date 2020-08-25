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
                DescriptorContext context = CreateContext(builder);

                IBindingLookup bindingLookup =
                    builder._bindingCompiler.Compile(context);

                IReadOnlyList<ITypeReference> typeReferences =
                    CreateTypeReferences(builder, context, bindingLookup);

                TypeInitializer initializer =
                    InitializeTypes(builder, context, bindingLookup, typeReferences, lazySchema);

                return CompleteSchema(builder, context, lazySchema, initializer);
            }

            private static DescriptorContext CreateContext(
                SchemaBuilder builder)
            {
                DescriptorContext context = DescriptorContext.Create(
                    builder._options,
                    builder._services ?? new EmptyServiceProvider(),
                    builder._conventions,
                    builder._contextData);

                foreach (Action<IDescriptorContext> action in builder._onBeforeCreate)
                {
                    action(context);
                }

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
                    types.Add(typeRef(context.Inspector));
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
                    types.Add(builder._schema(context.Inspector));
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
                        ti => TypeReference.Create(typeName, TypeContext.Output));
                }
            }

            private static TypeInitializer InitializeTypes(
                SchemaBuilder builder,
                DescriptorContext context,
                IBindingLookup bindingLookup,
                IReadOnlyList<ITypeReference> types,
                LazySchema lazySchema)
            {
                TypeInitializer initializer =
                    CreateTypeInitializer(builder, context, bindingLookup, types);
                initializer.Initialize(() => lazySchema.Schema, builder._options);
                return initializer;
            }

            private static TypeInitializer CreateTypeInitializer(
                SchemaBuilder builder,
                IDescriptorContext context,
                IBindingLookup bindingLookup,
                IEnumerable<ITypeReference> typeReferences)
            {
                Dictionary<OperationType, ITypeReference> operations =
                    builder._operations.ToDictionary(t => t.Key, t => t.Value(context.Inspector));

                var interceptor = new AggregateTypeInitializationInterceptor(
                    CreateInterceptors(builder, context.Services));

                var initializer = new TypeInitializer(
                    context,
                    typeReferences,
                    builder._resolverTypes,
                    interceptor,
                    builder._isOfType,
                    type => IsQueryType(context.Inspector, type, operations));

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
                    initializer.ClrTypes[(ClrTypeReference)binding.Value.Item1(context.Inspector)] =
                        binding.Value.Item2.Invoke(context.Inspector);
                }

                return initializer;
            }

            private static IReadOnlyCollection<ITypeInitializationInterceptor> CreateInterceptors(
                SchemaBuilder builder,
                IServiceProvider services)
            {
                var list = new List<ITypeInitializationInterceptor>();

                if (services is not EmptyServiceProvider)
                {
                    var inter = services.GetService<IEnumerable<ITypeInitializationInterceptor>>();
                    if (inter is not null)
                    {
                        list.AddRange(inter);
                    }
                }

                if (builder._interceptors.Count > 0)
                {
                    var serviceFactory = new ServiceFactory { Services = services };

                    foreach (object interceptorOrType in builder._interceptors)
                    {
                        if (interceptorOrType is Type type)
                        {
                            var obj = serviceFactory.CreateInstance(type);
                            if (obj is ITypeInitializationInterceptor casted)
                            {
                                list.Add(casted);
                            }
                        }
                        else if (interceptorOrType is ITypeInitializationInterceptor interceptor)
                        {
                            list.Add(interceptor);
                        }
                    }
                }

                return list;
            }

            private static bool IsQueryType(
                ITypeInspector typeInspector,
                TypeSystemObjectBase type,
                Dictionary<OperationType, ITypeReference> operations)
            {
                if (type is ObjectType objectType)
                {
                    if (operations.TryGetValue(OperationType.Query, out ITypeReference? reference))
                    {
                        if (reference is SchemaTypeReference sr)
                        {
                            return sr.Type == objectType;
                        }

                        if (reference is ClrTypeReference cr)
                        {
                            return cr.Type == typeInspector.GetType(objectType.GetType())
                                   || cr.Type == typeInspector.GetType(objectType.RuntimeType);
                        }

                        if (reference is SyntaxTypeReference str)
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

            private static Schema CompleteSchema(
                SchemaBuilder builder,
                DescriptorContext context,
                LazySchema lazySchema,
                TypeInitializer typeInitializer)
            {
                SchemaTypesDefinition definition =
                    CreateSchemaDefinition(builder, context, typeInitializer);

                if (definition.QueryType == null && builder._options.StrictValidation)
                {
                    throw new SchemaException(
                        SchemaErrorBuilder.New()
                            .SetMessage(TypeResources.SchemaBuilder_NoQueryType)
                            .Build());
                }

                Schema schema = typeInitializer.DiscoveredTypes!.Types
                    .Select(t => t.Type).OfType<Schema>().First();

                schema.CompleteSchema(definition);
                lazySchema.Schema = schema;
                return schema;
            }

            private static SchemaTypesDefinition CreateSchemaDefinition(
                SchemaBuilder builder,
                DescriptorContext context,
                TypeInitializer typeInitializer)
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
                    builder._operations.ToDictionary(t => t.Key, t => t.Value(context.Inspector));


                definition.QueryType = ResolveOperation(
                    OperationType.Query, operations, typeInitializer);
                definition.QueryType = ResolveOperation(
                    OperationType.Mutation, operations, typeInitializer);
                definition.QueryType = ResolveOperation(
                    OperationType.Subscription, operations, typeInitializer);

                IReadOnlyCollection<TypeSystemObjectBase> types =
                    RemoveUnreachableTypes(builder, typeInitializer.DiscoveredTypes!, definition);

                definition.Types = types.OfType<INamedType>().Distinct().ToArray();
                definition.DirectiveTypes = types.OfType<DirectiveType>().Distinct().ToArray();

                return definition;
            }

            private static ObjectType? ResolveOperation(
                OperationType operation,
                Dictionary<OperationType, ITypeReference> operations,
                TypeInitializer initializer)
            {
                if (!operations.ContainsKey(operation))
                {
                    NameString typeName = operation.ToString();
                    return initializer.DiscoveredTypes!.Types
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

                    if (reference is ClrTypeReference cr &&
                        initializer.TryGetRegisteredType(cr, out RegisteredType? registeredType))
                    {
                        return (ObjectType)registeredType.Type;
                    }

                    if (reference is SyntaxTypeReference str)
                    {
                        NamedTypeNode namedType = str.Type.NamedType();
                        return initializer.DiscoveredTypes!.Types
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
                DiscoveredTypes discoveredTypes,
                SchemaTypesDefinition definition)
            {
                if (builder._options.RemoveUnreachableTypes)
                {
                    var trimmer = new TypeTrimmer(discoveredTypes);

                    if (definition.QueryType is { })
                    {
                        trimmer.VisitRoot(definition.QueryType);
                    }

                    if (definition.MutationType is { })
                    {
                        trimmer.VisitRoot(definition.MutationType);
                    }

                    if (definition.SubscriptionType is { })
                    {
                        trimmer.VisitRoot(definition.SubscriptionType);
                    }

                    return trimmer.Types;
                }

                return discoveredTypes.Types.Select(t => t.Type).ToList();
            }
        }
    }
}
