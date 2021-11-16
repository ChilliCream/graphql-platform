using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Lodash
{
    public class AggregationVisitorContext : ISyntaxVisitorContext
    {
        public AggregationVisitorContext(ISyntaxNode rootSyntaxNode, ISchema schema)
        {
            Steps = new Stack<IList<AggregationRewriterStep>>();
            SyntaxNodes = new List<ISyntaxNode>() { rootSyntaxNode };
            Operations = new List<IReadOnlyList<AggregationOperation>>();
            Operations.Push(Array.Empty<AggregationOperation>());
            Steps.Push(new List<AggregationRewriterStep>());
            Schema = schema;
            Directives = schema.DirectiveTypes
                .OfType<IAggregationDirectiveType>()
                .ToDictionary(x => x.Name.Value);
        }

        public ISchema Schema { get; }

        public IDictionary<string, IAggregationDirectiveType> Directives { get; }

        public Stack<IList<AggregationRewriterStep>> Steps { get; }

        public IList<ISyntaxNode> SyntaxNodes { get; }

        public List<IReadOnlyList<AggregationOperation>> Operations { get; }
    }
}
