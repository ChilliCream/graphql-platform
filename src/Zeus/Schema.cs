using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using Zeus.Definitions;
using Zeus.Resolvers;

namespace Zeus
{
    public class Schema
        : ISchema
    {
        private readonly Dictionary<string, ObjectTypeDefinition> _objectTypes;
        private readonly Dictionary<string, InputObjectTypeDefinition> _inputTypes;
        private readonly ObjectTypeDefinition _queryType;
        private readonly ObjectTypeDefinition _mutationType;
        private readonly IResolverCollection _resolvers;

        private Schema(IEnumerable<ObjectTypeDefinition> objectTypes,
            IEnumerable<InputObjectTypeDefinition> inputTypes,
            IResolverCollection resolvers)
        {
            _objectTypes = objectTypes.ToDictionary(t => t.Name);
            _inputTypes = inputTypes.ToDictionary(t => t.Name);
            _resolvers = resolvers;

            _objectTypes.TryGetValue(WellKnownTypes.Query,
                out ObjectTypeDefinition _queryType);
            _objectTypes.TryGetValue(WellKnownTypes.Mutation,
                out ObjectTypeDefinition _mutationType);
        }

        public ObjectTypeDefinition Query => _queryType;
        public ObjectTypeDefinition Mutation => _mutationType;
        public IResolverCollection Resolvers => _resolvers;

        public bool TryGetObjectType(string typeName, out ObjectTypeDefinition objectType)
        {
            return _objectTypes.TryGetValue(typeName, out objectType);
        }

        public bool TryGetInputType(string typeName, out InputObjectTypeDefinition inputType)
        {
            return _inputTypes.TryGetValue(typeName, out inputType);
        }

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

            SchemaSyntaxVisitor userSchemaDefinitions = LoadSchema(schema);

            /* 
                if (userSchemaDefinitions.ObjectTypes.Count == 0)
                {
                    throw new ArgumentException("The specified schema contains no type declarations.", nameof(schema));
                }

                if (!userSchemaDefinitions.HasQueryType)
                {
                    throw new ArgumentException("The specified schema does not contain the required 'Query' type.", nameof(schema));
                }

                SchemaSyntaxVisitor introspectionSchemaDefinitions = LoadSchema(Introspection.Introspection.Schema);


                return new Schema(userSchemaDefinitions.ObjectTypes, userSchemaDefinitions.InputTypes, resolvers);

            */
            throw new NotImplementedException();
        }

        private static SchemaSyntaxVisitor LoadSchema(string schema)
        {
            Source source = new Source(schema);
            Parser parser = new Parser(new Lexer());

            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor();
            parser.Parse(source).Accept(visitor);

            return visitor;
        }


    }
}

