using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using Zeus.Abstractions;
using Zeus.Resolvers;
using Zeus.Parser;
using System.Collections.Immutable;

namespace Zeus
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

        public static Schema Create(string schema, Action<IResolverBuilder> configure)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            IResolverBuilder resolverBuilder = ResolverBuilder.Create();
            configure(resolverBuilder);
            return Create(schema, resolverBuilder.Build());
        }

        public static Schema Create(string schema, IResolverCollection resolvers)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (resolvers == null)
            {
                throw new ArgumentNullException(nameof(resolvers));
            }

            SchemaDocumentReader schemaReader = new SchemaDocumentReader();
            SchemaDocument schemaDocument = schemaReader.Read(schema, _intospectionSchema);
            // validate schema!

            return new Schema(schemaDocument, resolvers);
        }

        public IType InferType(ObjectTypeDefinition typeDefinition, FieldDefinition fieldDefinition, object obj)
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

            if (type.IsDefined(typeof(GraphQLNameAttribute), false))
            {
                GraphQLNameAttribute attribute = type.GetCustomAttributes(typeof(GraphQLNameAttribute), false)
                    .OfType<GraphQLNameAttribute>().First();
                name = new NamedType(attribute.Name);
            }
            else
            {
                name = new NamedType(type.Name);
            }

            lock (_sync)
            {
                _typeNameCache = _typeNameCache.SetItem(type, name);
            }

            return name;
        }
    }
}