using System;

namespace HotChocolate.CodeGeneration
{
    public class SourceFile
    {
        public string Name { get; }

        public string Source { get; }

        public SourceFile(string name, string source)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }
    }
}
