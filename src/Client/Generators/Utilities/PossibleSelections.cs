using System;
using System.Collections.Generic;

namespace StrawberryShake.Generators.Utilities
{
    internal sealed class PossibleSelections
    {
        public PossibleSelections(SelectionInfo returnType)
        {
            ReturnType = returnType;
            Variants = new List<SelectionInfo> { returnType };
        }

        public PossibleSelections(
            SelectionInfo returnType,
            IReadOnlyList<SelectionInfo> variants)
        {
            ReturnType = returnType;
            Variants = variants;
        }

        public SelectionInfo ReturnType { get; }

        public IReadOnlyList<SelectionInfo> Variants { get; }
    }
}
