using System.Threading;
using System.IO;
using System.Globalization;
using System.Linq;
using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Merge;
using System.Threading.Tasks;

namespace StrawberryShake.Generators
{
    public class ClientGenerator
    {
        private readonly Dictionary<string, DocumentNode> _schemas =
            new Dictionary<string, DocumentNode>();
        private readonly List<DocumentNode> _extensions =
            new List<DocumentNode>();
        private readonly Dictionary<string, DocumentNode> _queries =
            new Dictionary<string, DocumentNode>();

        public ClientGenerator SetOutput(string directoryName)
        {
            throw new NotImplementedException();
        }

        public ClientGenerator SetOutput(IFileHandler output)
        {
            throw new NotImplementedException();
        }

        public ClientGenerator AddSchemaDocument(string fileName)
        {
            return AddSchemaDocument(
                Path.GetFileNameWithoutExtension(fileName),
                Utf8GraphQLParser.Parse(
                    File.ReadAllBytes(fileName)));
        }

        public ClientGenerator AddSchemaDocument(
            string name, DocumentNode document)
        {
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
            throw new NotImplementedException();
        }

        public ClientGenerator AddQueryDocument(DocumentNode document)
        {
            throw new NotImplementedException();
        }

        public Task CreateAsync()
        {
            DocumentNode mergedSchema = MergeSchema();
            mergedSchema = MergeSchemaExtensions(mergedSchema);

            var queryCollection = new QueryCollection();
            return null;
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
    }
}
