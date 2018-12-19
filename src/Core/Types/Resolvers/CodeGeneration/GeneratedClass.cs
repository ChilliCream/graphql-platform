using System;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal class GeneratedClass
    {
        public GeneratedClass(string @namespace, string className, string sourceText)
        {
            if (string.IsNullOrEmpty(className))
            {
                throw new ArgumentNullException(nameof(className));
            }

            if (string.IsNullOrEmpty(sourceText))
            {
                throw new ArgumentNullException(nameof(sourceText));
            }

            ClassName = className;
            SourceText = sourceText;
            Namespace = @namespace;
            FullName = @namespace + "." + className;
        }

        public string Namespace { get; }
        public string ClassName { get; }
        public string FullName { get; }
        public string SourceText { get; }
    }
}
