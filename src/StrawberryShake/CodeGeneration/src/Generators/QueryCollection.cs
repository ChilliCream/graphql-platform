using System.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;
using HotChocolate;
using HCErrorBuilder = HotChocolate.ErrorBuilder;

namespace StrawberryShake.Generators
{
    public class QueryCollection
        : IEnumerable<IQueryDescriptor>
    {
        private readonly List<IQueryDescriptor> _descriptors =
            new List<IQueryDescriptor>();
        private readonly IDocumentHashProvider _hashProvider;
        private readonly string _namespace;

        public QueryCollection(string ns)
        {
            _hashProvider = new MD5DocumentHashProvider();
            _namespace = ns ?? throw new ArgumentNullException(nameof(ns));
        }

        public QueryCollection(IDocumentHashProvider hashProvider, string ns)
        {
            _hashProvider = hashProvider
                ?? throw new ArgumentNullException(nameof(hashProvider));
            _namespace = ns ?? throw new ArgumentNullException(nameof(ns));
        }

        public Task<IQueryDescriptor> LoadFromDocumentAsync(
            string name, string fileName, DocumentNode document)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (fileName is null)
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return LoadAsync(name, fileName, document);
        }

        private async Task<IQueryDescriptor> LoadAsync(
            string name, string fileName, DocumentNode document)
        {
            OperationDefinitionNode operation =
                document.Definitions.OfType<OperationDefinitionNode>()
                    .FirstOrDefault(t => t.Name is null);

            if (operation != null)
            {
                throw new GeneratorException(HCErrorBuilder.New()
                    .SetMessage("All operations have to have a name in order " +
                        "to work with Strawberry Shake. Check the specified " +
                        "operation and give it a name, then retry generating " +
                        "the client.")
                    .SetCode("OPERATION_NO_NAME")
                    .AddLocation(operation)
                    .SetExtension("fileName", fileName)
                    .Build());
            }

            DocumentNode rewritten = AddTypeNameQueryRewriter.Rewrite(document);
            byte[] rewrittenBuffer;

            var serializer = new QuerySyntaxSerializer(false);

            using (var stream = new MemoryStream())
            {
                using (var sw = new StreamWriter(stream))
                {
                    using (var writer = new DocumentWriter(sw))
                    {
                        serializer.Visit(rewritten, writer);
                    }
                }

                await stream.FlushAsync().ConfigureAwait(false);
                rewrittenBuffer = stream.ToArray();
            }

            string hash = _hashProvider.ComputeHash(rewrittenBuffer);

            var descriptor = new QueryDescriptor(
                name,
                _namespace,
                _hashProvider.Name,
                hash,
                rewrittenBuffer,
                document);
            _descriptors.Add(descriptor);
            return descriptor;
        }

        public IEnumerator<IQueryDescriptor> GetEnumerator() =>
            _descriptors.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() =>
            GetEnumerator();
    }
}
