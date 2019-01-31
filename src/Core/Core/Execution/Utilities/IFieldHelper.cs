using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    public interface IFieldHelper
        : IFieldCollector
    {
        FieldDelegate CreateMiddleware(FieldSelection fieldSelection);
    }

    public interface IFieldCollector
    {
        IReadOnlyCollection<FieldSelection> CollectFields(
            ObjectType objectType,
            SelectionSetNode selectionSet);
    }
}
