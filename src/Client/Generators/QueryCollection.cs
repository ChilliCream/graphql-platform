using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HotChocolate.Language;
using StrawberryShake.Generators.Descriptors;
using StrawberryShake.Generators.Utilities;

namespace StrawberryShake.Generators
{
    public class QueryCollection
        : IEnumerable<IQueryDescriptor>
    {
        private readonly List<IQueryDescriptor> _descriptors =
            new List<IQueryDescriptor>();
        private readonly IDocumentHashProvider _hashProvider;

        public QueryCollection()
        {
            _hashProvider = new MD5DocumentHashProvider();
        }

        public QueryCollection(IDocumentHashProvider hashProvider)
        {
            _hashProvider = hashProvider
                ?? throw new ArgumentNullException(nameof(hashProvider));
        }

        public Task<IQueryDescriptor> LoadQuery(string file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return LoadInternalAsync(file);
        }

        public async Task<IQueryDescriptor> LoadInternalAsync(string file)
        {
            byte[] documentBuffer = await File.ReadAllBytesAsync(file);
            DocumentNode document = Utf8GraphQLParser.Parse(documentBuffer);
            DocumentNode rewritten = AddTypeNameQueryRewriter.Rewrite(document);

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

                await stream.FlushAsync();
                documentBuffer = stream.ToArray();
            }

            string hash = _hashProvider.ComputeHash(documentBuffer);

            var descriptor = new QueryDescriptor(
                Path.GetFileNameWithoutExtension(file),
                _hashProvider.Name,
                hash,
                documentBuffer,
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
