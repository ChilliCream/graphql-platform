using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using Zeus.Abstractions;
using Zeus.Resolvers;
using Zeus.Parser;

namespace Zeus
{
    public partial class Schema
        : ISchema
    {
        private readonly SchemaDocument _schemaDocument;
        private readonly IResolverCollection _resolvers;

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



    }
}

