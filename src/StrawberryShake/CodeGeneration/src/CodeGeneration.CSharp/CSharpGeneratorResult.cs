using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorResult
    {
        public CSharpGeneratorResult(
            IReadOnlyList<CSharpDocument> cSharpDocuments,
            IReadOnlyList<IError> errors)
        {
            CSharpDocuments = cSharpDocuments;
            Errors = errors;
        }

        public IReadOnlyList<CSharpDocument> CSharpDocuments;

        public IReadOnlyList<IError> Errors;

        public bool HasErrors() => Errors.Count > 0;
    }
}
