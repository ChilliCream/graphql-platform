using System;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class Fragment
    {
        public Fragment(IType typeCondition, SelectionSetNode selectionSet)
        {
            if (typeCondition == null)
            {
                throw new ArgumentNullException(nameof(typeCondition));
            }

            if (selectionSet == null)
            {
                throw new ArgumentNullException(nameof(selectionSet));
            }

            TypeCondition = typeCondition;
            SelectionSet = selectionSet;
        }

        public IType TypeCondition { get; }
        public SelectionSetNode SelectionSet { get; }
    }
}
