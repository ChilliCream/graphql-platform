using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface ICachedQuery
    {
        DocumentNode Document { get; }

        IReadOnlyList<FieldSelection> GetOrCollectFields(
            ObjectType type,
            SelectionSetNode selectionSet,
            Func<IReadOnlyList<FieldSelection>> collectFields);
    }
}
