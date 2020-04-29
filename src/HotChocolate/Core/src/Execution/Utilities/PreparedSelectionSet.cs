using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedSelectionSet
    {
        public PreparedSelectionSet(
            SelectionSetNode selectionSet, 
            ObjectType typeContext, 
            IReadOnlyList<IPreparedSelection> selections)
        {
            SelectionSet = selectionSet;
            TypeContext = typeContext;
            Selections = selections;
        }

        public SelectionSetNode SelectionSet { get; }

        public ObjectType TypeContext { get; }

        public IReadOnlyList<IPreparedSelection> Selections { get; }
    }
}
