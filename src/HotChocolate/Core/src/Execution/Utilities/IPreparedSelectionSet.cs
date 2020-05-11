using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal interface IPreparedSelectionSet
    {
        SelectionSetNode SelectionSet { get; }

        IEnumerable<ObjectType> GetPossibleTypes();

        IPreparedSelectionList GetSelections(ObjectType typeContext);
    }
}
