using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Lodash
{
    public class LodashVisitorContext : ISyntaxVisitorContext
    {
        public LodashVisitorContext(ISyntaxNode rootSyntaxNode)
        {
            Steps = new Stack<IList<LodashRewriterStep>>();
            SyntaxNodes = new List<ISyntaxNode>() { rootSyntaxNode };
            Operations = new List<IReadOnlyList<LodashOperation>>();
            Operations.Push(Array.Empty<LodashOperation>());
            Steps.Push(new List<LodashRewriterStep>());
        }

        public Stack<IList<LodashRewriterStep>> Steps { get; }

        public IList<ISyntaxNode> SyntaxNodes { get; }

        public List<IReadOnlyList<LodashOperation>> Operations { get; }
    }
}
