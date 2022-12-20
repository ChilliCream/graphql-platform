#nullable enable

using System.Collections.Generic;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

public interface ISelectionVariants
{
    int Id { get; }

    IEnumerable<IObjectType> GetPossibleTypes();

    ISelectionSet GetSelectionSet(IObjectType typeContext);
}
