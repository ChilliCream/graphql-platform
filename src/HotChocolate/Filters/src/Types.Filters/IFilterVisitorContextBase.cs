using System;
using System.Collections.Generic;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Types.Filters
{
    [Obsolete("Use HotChocolate.Data.")]
    public interface IFilterVisitorContextBase
        : ISyntaxVisitorContext
    {
        Stack<IType> Types { get; }

        Stack<IInputField> Operations { get; }
    }
}
