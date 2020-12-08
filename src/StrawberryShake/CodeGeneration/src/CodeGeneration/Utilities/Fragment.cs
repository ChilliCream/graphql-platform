using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.CodeGeneration.Utilities
{
    internal sealed class Fragment : IFragment
    {
        public Fragment(
            string name,
            FragmentKind kind,
            INamedType typeCondition,
            SelectionSetNode selectionSet)
        {
            Name = name;
            Kind = kind;
            TypeCondition = typeCondition ?? throw new ArgumentNullException(nameof(typeCondition));
            SelectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public string Name { get; }

        public FragmentKind Kind { get; }

        public INamedType TypeCondition { get; }

        public SelectionSetNode SelectionSet { get; }
    }
}
