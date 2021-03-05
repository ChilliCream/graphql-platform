using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorResult
    {
        public CSharpGeneratorResult()
            : this(Array.Empty<SourceDocument>(), Array.Empty<IError>())
        {
        }

        public CSharpGeneratorResult(
            IReadOnlyList<IError> errors)
            : this(Array.Empty<SourceDocument>(), errors)
        {
        }

        public CSharpGeneratorResult(
            IReadOnlyList<SourceDocument> documents)
            : this(documents, Array.Empty<IError>())
        {
        }

        public CSharpGeneratorResult(
            IReadOnlyList<SourceDocument> documents,
            IReadOnlyList<IError> errors)
        {
            Documents = documents;
            Errors = errors;
        }

        public IReadOnlyList<SourceDocument> Documents { get; }

        public IReadOnlyList<IError> Errors { get; }

        public bool HasErrors() => Errors.Count > 0;
    }
}
