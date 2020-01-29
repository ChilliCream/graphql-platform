using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.Generators.Descriptors
{
    public class QueryDescriptor
        : IQueryDescriptor
    {
        public QueryDescriptor(
            string name,
            string ns,
            string hashName,
            string hash,
            byte[] document,
            DocumentNode originalDocument)
        {
            Name = name
                ?? throw new ArgumentNullException(nameof(name));
            Namespace = ns ?? throw new ArgumentNullException(nameof(ns));
            HashName = hashName
                ?? throw new ArgumentNullException(nameof(hashName));
            Hash = hash
                ?? throw new ArgumentNullException(nameof(hash));
            Document = document
                ?? throw new ArgumentNullException(nameof(document));
            OriginalDocument = originalDocument
                ?? throw new ArgumentNullException(nameof(originalDocument));
        }

        public string Name { get; }

        public string Namespace { get; }

        public string HashName { get; }

        public string Hash { get; }

        public byte[] Document { get; }

        public DocumentNode OriginalDocument { get; }

        public IEnumerable<ICodeDescriptor> GetChildren()
        {
            yield break;
        }
    }
}
