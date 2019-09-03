using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        public Task<IQueryDescriptor> LoadFromFileAsync(string file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            return LoadFileAsync(file);
        }

        private async Task<IQueryDescriptor> LoadFileAsync(string file)
        {
            if (file is null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            byte[] documentBuffer =
                await Task.Run(() => File.ReadAllBytes(file));

            return await LoadAsync(
                Path.GetFileNameWithoutExtension(file),
                documentBuffer);
        }

        public Task<IQueryDescriptor> LoadFromStringAsync(
            string name, string query)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            byte[] documentBuffer = Encoding.UTF8.GetBytes(query);
            return LoadAsync(name, documentBuffer);
        }

        public Task<IQueryDescriptor> LoadFromDocumentAsync(
            string name, DocumentNode query)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            byte[] documentBuffer = Encoding.UTF8.GetBytes(
                QuerySyntaxSerializer.Serialize(query));
            return LoadAsync(name, documentBuffer);
        }

        private async Task<IQueryDescriptor> LoadAsync(
            string name, byte[] documentBuffer)
        {
            DocumentNode document = Utf8GraphQLParser.Parse(documentBuffer);
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

                await stream.FlushAsync();
                rewrittenBuffer = stream.ToArray();
            }

            string hash = _hashProvider.ComputeHash(documentBuffer);

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
