using System;
using System.Collections.Generic;
using HotChocolate;

namespace StrawberryShake.Generators.Utilities
{
    internal class FragmentNode
        : IFragmentNode
    {
        public FragmentNode(Fragment fragment)
        {
            Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        }

        public NameString Name => Fragment.Name;

        public IFragment Fragment { get; }

        public List<IFragmentNode> Children { get; } =
            new List<IFragmentNode>();

        IReadOnlyList<IFragmentNode> IFragmentNode.Children => Children;
    }
}
