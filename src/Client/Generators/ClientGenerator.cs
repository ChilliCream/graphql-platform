using System.Threading;
using System.IO;
using System.Globalization;
using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using System.Threading.Tasks;
using HotChocolate;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.CSharp;
using IOPath = System.IO.Path;

namespace StrawberryShake.Generators
{
    public class ClientGenerator
    {
        private static readonly ICodeGenerator[] _codeGenerators =
            new ICodeGenerator[]
            {
                new ClassGenerator(),
                new InputClassGenerator(),
                new InputClassSerializerGenerator(),
                new InterfaceGenerator(),
                new ResultParserGenerator()
            };
        private readonly Dictionary<string, DocumentNode> _schemas =
            new Dictionary<string, DocumentNode>();
        private readonly List<DocumentNode> _extensions =
            new List<DocumentNode>();
        private readonly Dictionary<string, DocumentNode> _queries =
            new Dictionary<string, DocumentNode>();
        private readonly Dictionary<string, Type> _scalarTypes =
            new Dictionary<string, Type>
            {
                { "String", typeof(string) },
                { "Int", typeof(int) },
                { "Float", typeof(double) },
                { "Boolean", typeof(bool) },
                { "ID", typeof(string) },
                { "Date", typeof(DateTime) },
                { "DateTime", typeof(DateTimeOffset) },
                { "Byte", typeof(byte) },
                { "Short", typeof(short) },
                { "Long", typeof(long) },
                { "Decimal", typeof(Decimal) },
                { "Uuid", typeof(Guid) },
                { "Guid", typeof(Guid) },
                { "Url", typeof(Uri) },
            };
        private IDocumentHashProvider _hashProvider;
        private readonly IFileHandler _output;

        public ClientGenerator SetOutput(string directoryName)
        {
            if (directoryName is null)
            {
                throw new ArgumentNullException(nameof(directoryName));
            }

            throw new NotImplementedException();
        }

        public ClientGenerator SetOutput(IFileHandler output)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            throw new NotImplementedException();
        }

        public ClientGenerator SetHashProvider(
            IDocumentHashProvider hashProvider)
        {
            _hashProvider = hashProvider;
            return this;
        }

        public ClientGenerator SetScalarType(string typeName, Type clrType)
        {
            if (typeName is null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (clrType is null)
            {
                throw new ArgumentNullException(nameof(clrType));
            }

            _scalarTypes[typeName] = clrType;
            return this;
        }

        public ClientGenerator AddSchemaDocument(string fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return AddSchemaDocument(
                IOPath.GetFileNameWithoutExtension(fileName),
                Utf8GraphQLParser.Parse(
                    File.ReadAllBytes(fileName)));
        }

        public ClientGenerator AddSchemaDocument(
            string name,
            DocumentNode document)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            var extensions = new HashSet<IDefinitionNode>(
                document.Definitions.OfType<ITypeExtensionNode>());

            if (extensions.Count == 0)
            {
                _schemas.Add(name, document);
                return this;
            }

            var types = extensions.Except(document.Definitions).ToList();

            if (types.Count > 0)
            {
                _schemas.Add(name, document.WithDefinitions(types));
            }

            _extensions.Add(document.WithDefinitions(extensions.ToList()));

            return this;
        }

        public ClientGenerator AddQueryDocument(string fileName)
        {
            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            return AddQueryDocument(
                IOPath.GetFileNameWithoutExtension(fileName),
                Utf8GraphQLParser.Parse(
                    File.ReadAllBytes(fileName)));
        }

        public ClientGenerator AddQueryDocument(
            string name,
            DocumentNode document)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            _queries.Add(name, document);
            return this;
        }

        public async Task CreateAsync()
        {
            if (_output is null)
            {
                throw new InvalidOperationException(
                    "You have to specify a field output handler before you " +
                    "can generate any client APIs.");
            }

            if (_schemas.Count == 0)
            {
                throw new InvalidOperationException(
                    "You have to specify at least one schema file before you " +
                    "can generate any client APIs.");
            }

            if (_queries.Count == 0)
            {
                throw new InvalidOperationException(
                    "You have to specify at least one query file before you " +
                    "can generate any client APIs.");
            }

            IDocumentHashProvider hashProvider = _hashProvider
                ?? new MD5DocumentHashProvider();

            // create schema
            DocumentNode mergedSchema = MergeSchema();
            mergedSchema = MergeSchemaExtensions(mergedSchema);
            ISchema schema = CreateSchema(mergedSchema);

            // parse queries
            IReadOnlyList<IQueryDescriptor> queries =
                await ParseQueriesAsync(hashProvider);

            // generate abstarct client models
            var usedNames = new HashSet<string>();
            var descriptors = new List<ICodeDescriptor>();
            var fieldTypes = new Dictionary<FieldNode, string>();

            GenerateModels(schema, queries, usedNames, descriptors, fieldTypes);

            var typeLookup = new TypeLookup(_scalarTypes, fieldTypes);

            // generate code from models
            foreach (ICodeDescriptor descriptor in descriptors)
            {
                ICodeGenerator generator =
                    _codeGenerators.First(t => t.CanHandle(descriptor));

                _output.WriteTo(
                    descriptor.Name + ".cs",
                    w => generator.WriteAsync(w, descriptor, typeLookup));
            }

            await _output.WriteAllAsync();
        }

        private DocumentNode MergeSchema()
        {
            SchemaMerger merger = SchemaMerger.New();

            foreach (KeyValuePair<string, DocumentNode> schema in _schemas)
            {
                merger.AddSchema(schema.Key, schema.Value);
            }

            return merger.Merge();
        }

        private DocumentNode MergeSchemaExtensions(DocumentNode schema)
        {
            var rewriter = new AddSchemaExtensionRewriter();
            DocumentNode currentSchema = schema;

            foreach (DocumentNode extension in _extensions)
            {
                currentSchema = rewriter.AddExtensions(
                    currentSchema, extension);
            }

            return currentSchema;
        }

        private static ISchema CreateSchema(DocumentNode schema)
        {
            return SchemaBuilder.New()
                .Use(next => context => Task.CompletedTask)
                .AddDocument(schema)
                .Create();
        }

        private async Task<IReadOnlyList<IQueryDescriptor>> ParseQueriesAsync(
            IDocumentHashProvider hashProvider)
        {
            var queryCollection = new QueryCollection(hashProvider);

            foreach (KeyValuePair<string, DocumentNode> query in _queries)
            {
                await queryCollection.LoadFromDocumentAsync(
                    query.Key, query.Value);
            }

            return queryCollection.ToList();
        }

        private static void GenerateModels(
            ISchema schema,
            IEnumerable<IQueryDescriptor> queries,
            ISet<string> usedNames,
            List<ICodeDescriptor> descriptors,
            Dictionary<FieldNode, string> fieldTypes)
        {
            foreach (IQueryDescriptor query in queries)
            {
                var modelGenerator = new CodeModelGenerator(
                    schema, query, usedNames);
                modelGenerator.Generate();

                descriptors.AddRange(modelGenerator.Descriptors);

                foreach (KeyValuePair<FieldNode, string> fieldType in
                    modelGenerator.FieldTypes)
                {
                    fieldTypes[fieldType.Key] = fieldType.Value;
                }
            }
        }
    }
}
