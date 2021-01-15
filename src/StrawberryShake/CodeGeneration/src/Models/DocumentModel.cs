using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Analyzers.Models
{
    public sealed class DocumentModel
    {
        private readonly byte[] _serializedDocument;

        public DocumentModel(
            string name,
            IReadOnlyList<OperationModel> operations,
            DocumentNode originalDocument,
            DocumentNode optimizedDocument,
            byte[] serializedDocument,
            string hashAlgorithm,
            string hash)
        {
            Name = name;
            Operations = operations;
            OriginalDocument = originalDocument;
            OptimizedDocument = optimizedDocument;
            _serializedDocument = serializedDocument;
            HashAlgorithm = hashAlgorithm;
            Hash = hash;
        }

        public string Name { get; }

        public IReadOnlyList<OperationModel> Operations { get; }

        public DocumentNode OriginalDocument { get; }

        public DocumentNode OptimizedDocument { get; }

        public ReadOnlySpan<byte> SerializedDocument => _serializedDocument;

        public string HashAlgorithm { get; }

        public string Hash { get; }
    }
}
