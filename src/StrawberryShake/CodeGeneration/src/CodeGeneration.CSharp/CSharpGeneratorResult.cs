using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorResult
    {
        public CSharpGeneratorResult()
            : this(Array.Empty<CSharpDocument>(), Array.Empty<IError>())
        {
        }

        public CSharpGeneratorResult(
            IReadOnlyList<IError> errors)
            : this(Array.Empty<CSharpDocument>(), errors)
        {
        }

        public CSharpGeneratorResult(
            IReadOnlyList<CSharpDocument> csharpDocuments)
            : this(csharpDocuments, Array.Empty<IError>())
        {
        }

        public CSharpGeneratorResult(
            IReadOnlyList<CSharpDocument> csharpDocuments,
            IReadOnlyList<IError> errors)
        {
            CSharpDocuments = csharpDocuments;
            Errors = errors;
        }

        public IReadOnlyList<CSharpDocument> CSharpDocuments;

        public IReadOnlyList<IError> Errors;

        public bool HasErrors() => Errors.Count > 0;
    }
}
