using System.Collections.Generic;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters
{
    public interface IFilterVisitorContextBase
        : ISyntaxVisitorContext
    {
        Stack<IType> Types { get; }

        Stack<IInputField> Operations { get; }

        IList<IError> Errors { get; }
    }
}