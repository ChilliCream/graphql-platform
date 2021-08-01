using System;

namespace HotChocolate.CodeGeneration
{
    public class SourceFile
    {
        /// <summary>
        /// The file name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The source code.
        /// </summary>
        public string Source { get; }

        public SourceFile(string fileName, string source)
        {
            Name = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }
    }
}
