using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface ISelectionVariants
    {
        SelectionSetNode SelectionSet { get; }

        IEnumerable<IObjectType> GetPossibleTypes();

        ISelectionSet GetSelectionSet(IObjectType typeContext);
    }
}
