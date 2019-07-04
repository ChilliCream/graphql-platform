using System.Threading.Tasks.Dataflow;
using System.Collections.Generic;
using Xunit;
using ChilliCream.Testing;
using Snapshooter.Xunit;

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

        [Fact]
        public void Visit_Kitchen_Sink_Query()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("kitchen-sink.graphql"));
            var visitationMap = new BarVisitationMap();

            // act
            document.Accept(new Bar(), visitationMap);

            // assert
            visitationMap.VisitedNodes.MatchSnapshot();
        }

        [Fact]
        public void Visit_Kitchen_Sink_Schema()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("schema-kitchen-sink.graphql"));
            var visitationMap = new BarVisitationMap();

            // act
            document.Accept(new Bar(), visitationMap);

            // assert
            visitationMap.VisitedNodes.MatchSnapshot();
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

        private class Bar
           : SyntaxNodeVisitor
        {
            public Bar()
                : base(VisitorAction.Continue)
            {

            }
        }

        private class BarVisitationMap
            : VisitationMap
        {
            public List<ISyntaxNode> VisitedNodes { get; } =
                new List<ISyntaxNode>();

            public override void ResolveChildren(
                ISyntaxNode node,
                IStack<SyntaxNodeInfo> children)
            {
                VisitedNodes.Add(node);
                base.ResolveChildren(node, children);
            }
        }
    }
}
