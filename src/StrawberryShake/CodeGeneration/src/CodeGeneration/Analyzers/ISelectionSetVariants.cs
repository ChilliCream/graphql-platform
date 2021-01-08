using System;
using System.Collections.Generic;

namespace StrawberryShake.CodeGeneration.Analyzers
{
    public class SelectionSetVariants
    {
        public SelectionSetVariants(
            SelectionSet returnType, 
            IReadOnlyList<SelectionSet>? variants = null)
        {
            ReturnType = returnType;
            Variants = variants ?? Array.Empty<SelectionSet>();
        }

        public SelectionSet ReturnType { get; }

        public IReadOnlyList<SelectionSet> Variants { get; }
    }
}
