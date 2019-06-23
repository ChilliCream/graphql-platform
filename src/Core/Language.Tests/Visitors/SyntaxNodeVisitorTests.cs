using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using Xunit;

namespace HotChocolate.Language
{
    public class SyntaxNodeVisitorTests
    {
        [Fact]
        public void AutoSkip()
        {
            var obj = new ObjectValueNode(
                new ObjectFieldNode("foo",
                    new StringValueNode("bar")));

            obj.Accept(new Foo());

        }


        private class Foo
            : SyntaxNodeVisitor
        {
            public Foo()
                : base(new Dictionary<NodeKind, VisitorAction>
                {
                    { NodeKind.ObjectValue, VisitorAction.Continue },
                    { NodeKind.ObjectField, VisitorAction.Continue }
                })
            {
            }

            public override VisitorAction Enter(
                StringValueNode node,
                ISyntaxNode parent,
                IReadOnlyList<object> path,
                IReadOnlyList<ISyntaxNode> ancestors)
            {
                return VisitorAction.Skip;
            }

            public override VisitorAction Leave(
                ObjectValueNode node,
                ISyntaxNode parent,
                IReadOnlyList<object> path,
                IReadOnlyList<ISyntaxNode> ancestors)
            {
                return VisitorAction.Skip;
            }
        }
    }
}
