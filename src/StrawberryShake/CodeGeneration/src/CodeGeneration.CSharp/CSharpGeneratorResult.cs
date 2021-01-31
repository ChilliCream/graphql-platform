using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorResult
    {
        public CSharpGeneratorResult(
            IReadOnlyList<CSharpDocument> cSharpDocuments,
            IReadOnlyList<HotChocolate.IError> errors)
        {
            CSharpDocuments = cSharpDocuments;
            Errors = errors;
        }

        public IReadOnlyList<CSharpDocument> CSharpDocuments;
        public IReadOnlyList<HotChocolate.IError> Errors;

        public bool HasErrors() => Errors.Count > 0;
    }
}
