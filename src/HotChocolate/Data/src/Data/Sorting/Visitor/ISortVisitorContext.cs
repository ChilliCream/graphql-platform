using System.Collections.Generic;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Data.Sorting
{
    public interface ISortVisitorContext
        : ISyntaxVisitorContext
    {
        Stack<IType> Types { get; }

        Stack<IInputField> Fields { get; }

        IList<IError> Errors { get; }
    }
}
