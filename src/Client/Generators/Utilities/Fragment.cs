using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace StrawberryShake.Generators.Utilities
{
    internal sealed class Fragment
        : IFragment
    {
        public Fragment(string name, INamedType typeCondition, SelectionSetNode selectionSet)
        {
            Name = name;
            TypeCondition = typeCondition ?? throw new ArgumentNullException(nameof(typeCondition));
            SelectionSet = selectionSet ?? throw new ArgumentNullException(nameof(selectionSet));
        }

        public string Name { get; }

        public INamedType TypeCondition { get; }

        public SelectionSetNode SelectionSet { get; }
    }
}
