using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public interface ISelectionVariants2
{
    int Id { get; }

    IEnumerable<IObjectType> GetPossibleTypes();

    ISelectionSet2 GetSelectionSet(IObjectType typeContext);
}
