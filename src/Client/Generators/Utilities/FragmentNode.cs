using System;
using System.Collections.Generic;

namespace StrawberryShake.Generators.Utilities
{
    internal class FragmentNode
        : IFragmentNode
    {
        public FragmentNode()
        {
        }

        public FragmentNode(Fragment fragment)
        {
            Fragment = fragment ?? throw new ArgumentNullException(nameof(fragment));
        }

        public IFragment Fragment { get; }

        public List<IFragmentNode> Children { get; } =
            new List<IFragmentNode>();

        IReadOnlyList<IFragmentNode> IFragmentNode.Children => Children;

        public FragmentNode AddChild(Fragment fragment)
        {
            var child = new FragmentNode(fragment);
            Children.Add(child);
            return child;
        }
    }
}
