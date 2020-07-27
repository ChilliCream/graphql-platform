using System.Collections.Generic;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContextBase
        : ISyntaxVisitorContext
    {
        IStackableList<IType> Types { get; }

        IStackableList<IInputField> Operations { get; }

        IList<IError> Errors { get; }
    }
}