using System;
using System.Collections.Generic;
using System.Linq;
using GraphQLParser;
using Zeus.Types;

namespace Zeus
{
    public class Schema
    {
        private readonly Dictionary<string, ObjectDeclaration> _objectTypes;
        private readonly Dictionary<string, InputDeclaration> _inputTypes;
        private readonly ObjectDeclaration _queryType;
        private readonly ObjectDeclaration _mutationType;
        private readonly IResolverCollection _resolvers;

        private Schema(IEnumerable<ObjectDeclaration> objectTypes,
            IEnumerable<InputDeclaration> inputTypes,
            IResolverCollection resolvers)
        {
            _objectTypes = objectTypes.ToDictionary(t => t.Name);
            _inputTypes = inputTypes.ToDictionary(t => t.Name);
            _resolvers = resolvers;

            _objectTypes.TryGetValue(WellKnownTypes.Query,
                out ObjectDeclaration _queryType);
            _objectTypes.TryGetValue(WellKnownTypes.Mutation,
                out ObjectDeclaration _mutationType);
        }

        public ObjectDeclaration Query => _queryType;
        public ObjectDeclaration Mutation => _mutationType;

        public bool TryGetObjectType(string typeName, out ObjectDeclaration objectType)
        {
            return _objectTypes.TryGetValue(typeName, out objectType);
        }

        public bool TryGetInputType(string typeName, out InputDeclaration inputType)
        {
            return _inputTypes.TryGetValue(typeName, out inputType);
        }

        public bool TryGetResolver(string typeName, string fieldName, out IResolver resolver)
        {
            return _resolvers.TryGetResolver(typeName, fieldName, out resolver);
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

            Source source = new Source(schema);
            Parser parser = new Parser(new Lexer());

            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor();
            parser.Parse(source).Accept(visitor);

            if (visitor.ObjectTypes.Count == 0)
            {
                throw new ArgumentException("The specified schema contains no type declarations.", nameof(schema));
            }

            if (!visitor.HasQueryType)
            {
                throw new ArgumentException("The specified schema does not contain the required 'Query' type.", nameof(schema));
            }

            return new Schema(visitor.ObjectTypes, visitor.InputTypes, resolvers);
        }
    }
}

