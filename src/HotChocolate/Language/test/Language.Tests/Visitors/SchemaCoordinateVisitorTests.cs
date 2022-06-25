using System.Collections.Generic;
using Xunit;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Language;

public class SchemaCoordinateVisitorTests
{
    [Fact]
    public static void VisitAllNodes()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, false, new("Abc"), new("def"), new("ghi"));

        // act
        var list = new List<string>();

        SyntaxVisitor
            .Create(
                current =>
                {
                    if (current is NameNode n)
                    {
                        list.Add(n.Value);
                    }
                    return SyntaxVisitor.Continue;
                },
                options: new() { VisitNames = true })
            .Visit(node);

        // assert
        Assert.Collection(
            list,
            s => Assert.Equal("Abc", s),
            s => Assert.Equal("def", s),
            s => Assert.Equal("ghi", s));
    }

    [Fact]
    public static void VisitAllNodes_With_Walker()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, false, new("Abc"), new("def"), new("ghi"));

        // act
        var list = new List<string>();
        var walker = new CustomSyntaxWalker(list);
        walker.Visit(node);

        // assert
        Assert.Collection(
            list,
            s => Assert.Equal("Abc", s),
            s => Assert.Equal("def", s),
            s => Assert.Equal("ghi", s));
    }

    [Fact]
    public static void VisitAllNodes_With_Generic_Walker()
    {
        // arrange
        var node = new SchemaCoordinateNode(null, false, new("Abc"), new("def"), new("ghi"));

        // act
        var list = new List<string>();
        var context = new CustomContext(list);
        var walker = new CustomGenericSyntaxWalker();
        walker.Visit(node, context);

        // assert
        Assert.Collection(
            list,
            s => Assert.Equal("Abc", s),
            s => Assert.Equal("def", s),
            s => Assert.Equal("ghi", s));
    }

    public class CustomSyntaxWalker : SyntaxWalker
    {
        private readonly List<string> _list;

        public CustomSyntaxWalker(List<string> list)
            : base(new() { VisitNames = true })
        {
            _list = list;
        }

        protected override ISyntaxVisitorAction Enter(NameNode node, ISyntaxVisitorContext context)
        {
            _list.Add(node.Value);
            return DefaultAction;
        }
    }

    public class CustomGenericSyntaxWalker : SyntaxWalker<CustomContext>
    {
        public CustomGenericSyntaxWalker()
            : base(new() { VisitNames = true })
        {
        }

        protected override ISyntaxVisitorAction Enter(NameNode node, CustomContext context)
        {
            context.List.Add(node.Value);
            return DefaultAction;
        }
    }

    public class CustomContext : ISyntaxVisitorContext
    {
        public CustomContext(List<string> list)
        {
            List = list;
        }

        public List<string> List { get; }
    }
}
