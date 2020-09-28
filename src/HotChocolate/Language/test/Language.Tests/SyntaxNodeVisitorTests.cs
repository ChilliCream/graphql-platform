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
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks());
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
                FileResource.Open("schema-kitchen-sink.graphql")
                    .NormalizeLineBreaks());
            var visitationMap = new BarVisitationMap();

            // act
            document.Accept(new Bar(), visitationMap);

            // assert
            visitationMap.VisitedNodes.MatchSnapshot();
        }

        [Fact]
        public void Visit_Kitchen_Sink_Schema_Names_With_Delegate()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("schema-kitchen-sink.graphql")
                    .NormalizeLineBreaks());
            var visitationMap = new BarVisitationMap();
            var enterNames = new List<string>();
            var leaveNames = new List<string>();

            // act
            document.Accept<NameNode>(
                (node, parent, path, ancestors) =>
                {
                    enterNames.Add(node.Value);
                    return VisitorAction.Continue;
                },
                (node, parent, path, ancestors) =>
                {
                    leaveNames.Add(node.Value);
                    return VisitorAction.Continue;
                },
                node => VisitorAction.Continue);

            // assert
            Assert.Equal(enterNames, leaveNames);
            new List<string>[] { enterNames, leaveNames }.MatchSnapshot();
        }

        [Fact]
        public void Visit_Kitchen_Sink_Query_Names_With_Delegate()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks());
            var visitationMap = new BarVisitationMap();
            var enterNames = new List<string>();
            var leaveNames = new List<string>();

            // act
            document.Accept<NameNode>(
                (node, parent, path, ancestors) =>
                {
                    enterNames.Add(node.Value);
                    return VisitorAction.Continue;
                },
                (node, parent, path, ancestors) =>
                {
                    leaveNames.Add(node.Value);
                    return VisitorAction.Continue;
                },
                node => VisitorAction.Continue);

            // assert
            Assert.Equal(enterNames, leaveNames);
            new List<string>[] { enterNames, leaveNames }.MatchSnapshot();
        }

        [Fact]
        public void Visit_Kitchen_Sink_Query_Names_With_Delegate_OnlyEnter()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks());
            var visitationMap = new BarVisitationMap();
            var visitedNames = new List<string>();

            // act
            document.Accept<NameNode>(
                (node, parent, path, ancestors) =>
                {
                    visitedNames.Add(node.Value);
                    return VisitorAction.Continue;
                },
                null,
                node => VisitorAction.Continue);

            // assert
            visitedNames.MatchSnapshot();
        }

        [Fact]
        public void Visit_Kitchen_Sink_Query_Names_With_Delegate_OnlyLeave()
        {
            // arrange
            DocumentNode document = Utf8GraphQLParser.Parse(
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks());
            var visitationMap = new BarVisitationMap();
            var visitedNames = new List<string>();

            // act
            document.Accept<NameNode>(
                null,
                (node, parent, path, ancestors) =>
                {
                    visitedNames.Add(node.Value);
                    return VisitorAction.Continue;
                },
                node => VisitorAction.Continue);

            // assert
            visitedNames.MatchSnapshot();
        }

        private class Foo
            : SyntaxNodeVisitor
        {
            public Foo()
                : base(new Dictionary<SyntaxKind, VisitorAction>
                {
                    { SyntaxKind.ObjectValue, VisitorAction.Continue },
                    { SyntaxKind.ObjectField, VisitorAction.Continue }
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
                IList<SyntaxNodeInfo> children)
            {
                VisitedNodes.Add(node);
                base.ResolveChildren(node, children);
            }
        }
    }
}
