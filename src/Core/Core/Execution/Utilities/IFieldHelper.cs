using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IFieldHelper
    {
        IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType,
            SelectionSetNode selectionSet);

        ExecuteMiddleware CreateMiddleware(
            ObjectType objectType,
            FieldNode fieldNode);
    }
}
