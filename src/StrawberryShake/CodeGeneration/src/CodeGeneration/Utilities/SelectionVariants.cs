using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class SelectionVariants
    {
        public SelectionVariants(Selection returnType)
        {
            ReturnType = returnType;
            Variants = new List<Selection> { returnType };
        }

        public SelectionVariants(
            Selection returnType,
            IReadOnlyList<Selection> variants)
        {
            ReturnType = returnType;
            Variants = variants;
        }

        public Selection ReturnType { get; }

        public IReadOnlyList<Selection> Variants { get; }
    }
}
