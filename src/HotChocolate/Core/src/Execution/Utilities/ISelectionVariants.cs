using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    internal interface ISelectionVariants
    {
        SelectionSetNode SelectionSet { get; }

        IEnumerable<ObjectType> GetPossibleTypes();

        ISelectionSet GetSelectionSet(ObjectType typeContext);
    }
}
