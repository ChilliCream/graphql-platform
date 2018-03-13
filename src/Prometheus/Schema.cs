using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using GraphQLParser;
using Prometheus.Abstractions;
using Prometheus.Resolvers;
using Prometheus.Parser;
using Prometheus.Introspection;
using System.Reflection;

namespace Prometheus
{
    public partial class Schema
        : ISchema
    {
        private readonly object _sync = new object();
        private readonly SchemaDocument _schemaDocument;
        private readonly IResolverCollection _resolvers;
        private ImmutableDictionary<Type, NamedType> _typeNameCache = ImmutableDictionary<Type, NamedType>.Empty;

        private Schema(SchemaDocument schemaDocument,
            IResolverCollection resolvers)
        {
            _schemaDocument = schemaDocument;
            _resolvers = resolvers;
        }

        public IReadOnlyDictionary<string, InterfaceTypeDefinition> InterfaceTypes
            => _schemaDocument.InterfaceTypes;

        public IReadOnlyDictionary<string, EnumTypeDefinition> EnumTypes
            => _schemaDocument.EnumTypes;

        public IReadOnlyDictionary<string, ObjectTypeDefinition> ObjectTypes
            => _schemaDocument.ObjectTypes;

        public IReadOnlyDictionary<string, UnionTypeDefinition> UnionTypes
            => _schemaDocument.UnionTypes;

        public IReadOnlyDictionary<string, InputObjectTypeDefinition> InputObjectTypes
            => _schemaDocument.InputObjectTypes;

        public ObjectTypeDefinition QueryType
            => _schemaDocument.QueryType;

        public ObjectTypeDefinition MutationType
            => _schemaDocument.MutationType;

        public IResolverCollection Resolvers => _resolvers;

        public IType InferType(ObjectTypeDefinition typeDefinition,
            FieldDefinition fieldDefinition, object obj)
        {
            if (typeDefinition == null)
            {
                throw new ArgumentNullException(nameof(typeDefinition));
            }

            if (fieldDefinition == null)
            {
                throw new ArgumentNullException(nameof(fieldDefinition));
            }

            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            string schemaTypeName = fieldDefinition.Type.TypeName();
            if (_schemaDocument.UnionTypes.ContainsKey(schemaTypeName)
                || _schemaDocument.InterfaceTypes.ContainsKey(schemaTypeName))
            {
                return GetTypeName(obj);
            }

            return fieldDefinition.Type.NamedType();
        }

        private NamedType GetTypeName(object obj)
        {
            Type type = obj.GetType();
            if (_typeNameCache.TryGetValue(type, out var name))
            {
                return name;
            }

            name = GetTypeName(type);

            lock (_sync)
            {
                _typeNameCache = _typeNameCache.SetItem(type, name);
            }

            return name;
        }

        private NamedType GetTypeName(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute), false))
            {
                GraphQLNameAttribute attribute =
                    type.GetCustomAttribute<GraphQLNameAttribute>();
                return new NamedType(attribute.Name);
            }
            else
            {
                return new NamedType(type.Name);
            }
        }

        #region IEnumerable

        public IEnumerator<ITypeDefinition> GetEnumerator()
        {
            return _schemaDocument.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        public static Schema Create(string schema,
            Action<IResolverBuilder> configure)
        {
            return Create(schema, configure, DefaultServiceProvider.Instance);
        }

        public static Schema Create(string schema,
            Action<IResolverBuilder> configure,
            IServiceProvider serviceProvider)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            if (serviceProvider == null)
            {
                throw new ArgumentNullException(nameof(serviceProvider));
            }

            SchemaDocumentReader schemaReader = new SchemaDocumentReader();
            SchemaDocument schemaDocument = schemaReader.Read(schema)
                .WithIntrospectionSchema();

            IResolverBuilder resolverBuilder = ResolverBuilder.Create();
            configure(resolverBuilder);

            IResolverCollection resolvers = resolverBuilder
                .AddIntrospectionResolvers()
                .Build(schemaDocument, serviceProvider);

            return new Schema(schemaDocument, resolvers);
        }
    }
}