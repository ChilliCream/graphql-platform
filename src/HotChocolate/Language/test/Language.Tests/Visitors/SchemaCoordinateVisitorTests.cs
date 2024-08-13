using Xunit;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Language;

public static class SchemaCoordinateVisitorTests
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
                options: new() { VisitNames = true, })
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

    public class CustomSyntaxWalker(List<string> list)
        : SyntaxWalker(new() { VisitNames = true, })
    {
        protected override ISyntaxVisitorAction Enter(NameNode node, object? context)
        {
            list.Add(node.Value);
            return DefaultAction;
        }
    }

    public class CustomGenericSyntaxWalker()
        : SyntaxWalker<CustomContext>(new SyntaxVisitorOptions { VisitNames = true, })
    {
        protected override ISyntaxVisitorAction Enter(NameNode node, CustomContext context)
        {
            context.List.Add(node.Value);
            return DefaultAction;
        }
    }

    public class CustomContext(List<string> list)
    {
        public List<string> List { get; } = list;
    }
}
