using System;
using System.Collections.Generic;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Sorting
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface ISortVisitorContextBase
        : ISyntaxVisitorContext
    {
        Stack<IType> Types { get; }

        Stack<IInputField> Operations { get; }
    }
}
