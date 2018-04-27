using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class Fragment
    {
        public Fragment(IType type, SelectionSetNode selectionSet)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            Type = type;
            SelectionSet = selectionSet;
        }

        public IType TypeCondition { get; }
        public SelectionSetNode SelectionSet { get; }
        public IType Type { get; }
    }
}
