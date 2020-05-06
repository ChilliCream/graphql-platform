using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Execution
{
    internal sealed class VariableRewriter : SyntaxWalker<VariableRewriterContext>
    {
        public object Rewrite(VariableRewriterContext context, IValueNode value)
        {
            throw new NotImplementedException();
        }
    }

    internal class VariableRewriterContext : ISyntaxVisitorContext
    {
        public IVariableValueCollection Variables { get; set; } = default!;

        public List<IType> Types { get; } = new List<IType>();
    }
}
