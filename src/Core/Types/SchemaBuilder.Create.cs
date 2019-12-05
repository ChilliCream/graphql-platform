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

namespace HotChocolate
{
    public partial class SchemaBuilder
    {
        public Schema Create()
        {
            IServiceProvider services = _services
                ?? new EmptyServiceProvider();

            var descriptorContext = DescriptorContext.Create(
                _options,
                services,
                CreateConventions(services));

            IBindingLookup bindingLookup =
                 _bindingCompiler.Compile(descriptorContext);

            IReadOnlyCollection<ITypeReference> types =
                GetTypeReferences(services, bindingLookup);

            var lazy = new LazySchema();

            TypeInitializer initializer =
                InitializeTypes(
                    services,
                    descriptorContext,
                    bindingLookup,
                    types,
                    () => lazy.Schema);

            SchemaTypesDefinition definition =
                CreateSchemaDefinition(initializer);

            if (definition.QueryType == null && _options.StrictValidation)
            {
                throw new SchemaException(
                    SchemaErrorBuilder.New()
                        .SetMessage(TypeResources.SchemaBuilder_NoQueryType)
                        .Build());
            }

            Schema schema = initializer.Types.Values
                .Select(t => t.Type)
                .OfType<Schema>()
                .First();

            schema.CompleteSchema(definition);
            lazy.Schema = schema;
            return schema;
        }

        ISchema ISchemaBuilder.Create() => Create();

        private IReadOnlyCollection<ITypeReference> GetTypeReferences(
            IServiceProvider services,
            IBindingLookup bindingLookup)
        {
            var types = new List<ITypeReference>(_types);

            if (_documents.Count > 0)
            {
                types.AddRange(ParseDocuments(services, bindingLookup));
            }

            if (_schema == null)
            {
                types.Add(new SchemaTypeReference(new Schema()));
            }
            else
            {
                types.Add(_schema);
            }

            return types;
        }

        private IEnumerable<ITypeReference> ParseDocuments(
            IServiceProvider services,
            IBindingLookup bindingLookup)
        {
            var types = new List<ITypeReference>();

            foreach (LoadSchemaDocument fetchSchema in _documents)
            {
                // TODO: retrieve root type names
                DocumentNode schemaDocument = fetchSchema(services);

                var visitor = new SchemaSyntaxVisitor(bindingLookup);
                visitor.Visit(schemaDocument, null);
                types.AddRange(visitor.Types);

                RegisterOperationName(OperationType.Query,
                    visitor.QueryTypeName);
                RegisterOperationName(OperationType.Mutation,
                    visitor.MutationTypeName);
                RegisterOperationName(OperationType.Subscription,
                    visitor.SubscriptionTypeName);

                IReadOnlyCollection<DirectiveNode> directives =
                    visitor.Directives ?? Array.Empty<DirectiveNode>();

                if (_schema == null
                    && (directives.Count > 0
                    || visitor.Description != null))
                {
                    SetSchema(new Schema(d =>
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

        private void RegisterOperationName(
            OperationType operation,
            string typeName)
        {
            if (!_operations.ContainsKey(operation)
                && !string.IsNullOrEmpty(typeName))
            {
                _operations.Add(operation,
                    new SyntaxTypeReference(
                        new NamedTypeNode(typeName),
                        TypeContext.Output));
            }
        }

        private TypeInitializer InitializeTypes(
            IServiceProvider services,
            IDescriptorContext descriptorContext,
            IBindingLookup bindingLookup,
            IEnumerable<ITypeReference> types,
            Func<ISchema> schemaResolver)
        {
            var interceptor = new AggregateTypeInitilizationInterceptor(
                CreateInterceptors(services));

            var initializer = new TypeInitializer(
                services,
                descriptorContext,
                types,
                _resolverTypes,
                _contextData,
                interceptor,
                _isOfType,
                IsQueryType);

            foreach (FieldMiddleware component in _globalComponents)
            {
                initializer.GlobalComponents.Add(component);
            }

            foreach (FieldReference reference in _resolvers.Keys)
            {
                initializer.Resolvers[reference] = new RegisteredResolver(
                    typeof(object), _resolvers[reference]);
            }

            foreach (RegisteredResolver resolver in bindingLookup.Bindings
                .SelectMany(t => t.CreateResolvers()))
            {
                var reference = new FieldReference(
                    resolver.Field.TypeName,
                    resolver.Field.FieldName);
                initializer.Resolvers[reference] = resolver;
            }

            foreach (KeyValuePair<ITypeReference, ITypeReference> binding in
                _clrTypes)
            {
                initializer.ClrTypes[binding.Key] = binding.Value;
            }

            initializer.Initialize(schemaResolver, _options);
            return initializer;
        }

        private SchemaTypesDefinition CreateSchemaDefinition(
            TypeInitializer initializer)
        {
            var definition = new SchemaTypesDefinition();

            definition.Types = initializer.Types.Values
                .Select(t => t.Type)
                .OfType<INamedType>()
                .Distinct()
                .ToArray();

            definition.DirectiveTypes = initializer.Types.Values
                .Select(t => t.Type)
                .OfType<DirectiveType>()
                .Distinct()
                .ToArray();

            RegisterOperationName(OperationType.Query,
                _options.QueryTypeName);
            RegisterOperationName(OperationType.Mutation,
                _options.MutationTypeName);
            RegisterOperationName(OperationType.Subscription,
                _options.SubscriptionTypeName);

            definition.QueryType = ResolveOperation(
                initializer, OperationType.Query);
            definition.MutationType = ResolveOperation(
                initializer, OperationType.Mutation);
            definition.SubscriptionType = ResolveOperation(
                initializer, OperationType.Subscription);

            return definition;
        }

        private ObjectType ResolveOperation(
            TypeInitializer initializer,
            OperationType operation)
        {
            if (!_operations.ContainsKey(operation))
            {
                NameString typeName = operation.ToString();
                return initializer.Types.Values
                    .Select(t => t.Type)
                    .OfType<ObjectType>()
                    .FirstOrDefault(t => t.Name.Equals(typeName));
            }
            else if (_operations.TryGetValue(operation,
                out ITypeReference reference))
            {

                if (reference is ISchemaTypeReference sr)
                {
                    return (ObjectType)sr.Type;
                }

                if (reference is IClrTypeReference cr
                    && initializer.TryGetRegisteredType(cr,
                    out RegisteredType registeredType))
                {
                    return (ObjectType)registeredType.Type;
                }

                if (reference is ISyntaxTypeReference str)
                {
                    NamedTypeNode namedType = str.Type.NamedType();
                    return initializer.Types.Values
                        .Select(t => t.Type)
                        .OfType<ObjectType>()
                        .FirstOrDefault(t => t.Name.Equals(
                            namedType.Name.Value));
                }
            }

            return null;
        }

        private bool IsQueryType(TypeSystemObjectBase type)
        {
            if (type is ObjectType objectType)
            {
                if (_operations.TryGetValue(OperationType.Query,
                    out ITypeReference reference))
                {
                    if (reference is ISchemaTypeReference sr)
                    {
                        return sr.Type == objectType;
                    }

                    if (reference is IClrTypeReference cr)
                    {
                        return cr.Type == objectType.GetType()
                            || cr.Type == objectType.ClrType;
                    }

                    if (reference is ISyntaxTypeReference str)
                    {
                        return objectType.Name.Equals(
                            str.Type.NamedType().Name.Value);
                    }
                }
                else
                {
                    return type is ObjectType
                        && type.Name.Equals(WellKnownTypes.Query);
                }
            }

            return false;
        }

        private IReadOnlyCollection<ITypeInitializationInterceptor> CreateInterceptors(
            IServiceProvider services)
        {
            var list = new List<ITypeInitializationInterceptor>();

            var obj = services.GetService(typeof(IEnumerable<ITypeInitializationInterceptor>));
            if (obj is IEnumerable<ITypeInitializationInterceptor> interceptors)
            {
                list.AddRange(interceptors);
            }

            var serviceFactory = new ServiceFactory { Services = services };
            Type interceptorType = typeof(ITypeInitializationInterceptor);
            foreach (Type type in _interceptors.Where(t => interceptorType.IsAssignableFrom(t)))
            {
                obj = serviceFactory.CreateInstance(type);
                if (obj is ITypeInitializationInterceptor interceptor)
                {
                    list.Add(interceptor);
                }
            }

            return list;
        }

        private IReadOnlyDictionary<Type, IConvention> CreateConventions(
            IServiceProvider services)
        {
            var serviceFactory = new ServiceFactory { Services = services };
            var conventions = new Dictionary<Type, IConvention>();

            foreach (KeyValuePair<Type, CreateConvention> item in _conventions)
            {
                conventions.Add(item.Key, item.Value(serviceFactory));
            }

            return conventions;
        }
    }
}
