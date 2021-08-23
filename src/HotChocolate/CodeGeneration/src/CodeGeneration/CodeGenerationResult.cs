using System.Collections.Generic;

namespace HotChocolate.CodeGeneration
{
    public class CodeGenerationResult
    {
        public IList<SourceFile> SourceFiles { get; } = new List<SourceFile>();

        public void AddSource(string name, string source)
        {
            SourceFiles.Add(new SourceFile(name, source));
        }
    }
}
