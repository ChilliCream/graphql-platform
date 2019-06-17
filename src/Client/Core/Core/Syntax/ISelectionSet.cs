using System;
using System.Collections.Generic;
using System.Reflection;

namespace HotChocolate.Client.Core.Syntax
{
    public interface ISelectionSet : ISyntaxNode
    {
        IList<ISyntaxNode> Selections { get; }
    }
}
