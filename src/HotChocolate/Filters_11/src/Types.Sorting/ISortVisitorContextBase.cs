using System.Collections.Generic;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Sorting
{
    public interface ISortVisitorContextBase
        : ISyntaxVisitorContext
    {
        Stack<IType> Types { get; }

        Stack<IInputField> Operations { get; }
    }
}
