namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapSyntaxVisitorTests
{
    [Fact]
    public void VisitChildren_PathSegmentArguments_DescendsIntoArgumentValues()
    {
        // arrange
        var node = new FieldSelectionMapParser("width(unit: IMPERIAL)").Parse();
        var visitor = new CollectingVisitor();

        // act
        visitor.Visit(node, null);

        // assert
        Assert.Multiple(
            () => Assert.Contains(FieldSelectionMapSyntaxKind.Argument, visitor.EnteredKinds),
            () => Assert.Contains(FieldSelectionMapSyntaxKind.EnumValue, visitor.EnteredKinds));
    }

    [Fact]
    public void VisitChildren_ObjectFieldSelectionArguments_DescendsIntoArgumentValues()
    {
        // arrange
        var node = new FieldSelectionMapParser("{ width(unit: IMPERIAL) }").Parse();
        var visitor = new CollectingVisitor();

        // act
        visitor.Visit(node, null);

        // assert
        Assert.Multiple(
            () => Assert.Contains(FieldSelectionMapSyntaxKind.Argument, visitor.EnteredKinds),
            () => Assert.Contains(FieldSelectionMapSyntaxKind.EnumValue, visitor.EnteredKinds));
    }

    [Fact]
    public void VisitChildren_ArgumentValueWithEveryLeafKind_VisitsInDepthFirstOrder()
    {
        // arrange
        var node = new FieldSelectionMapParser(
            "f(a: { k: [1, 1.5, \"s\", true, null, SOME_ENUM] })").Parse();
        var visitor = new CollectingVisitor();

        // act
        visitor.Visit(node, null);

        // assert
        visitor.VisitLog.MatchInlineSnapshot(
            """
            Enter Path
            Enter PathSegment
            Enter Name
            Leave Name
            Enter Argument
            Enter Name
            Leave Name
            Enter ObjectValue
            Enter ObjectField
            Enter Name
            Leave Name
            Enter ListValue
            Enter IntValue
            Leave IntValue
            Enter FloatValue
            Leave FloatValue
            Enter StringValue
            Leave StringValue
            Enter BooleanValue
            Leave BooleanValue
            Enter NullValue
            Leave NullValue
            Enter EnumValue
            Leave EnumValue
            Leave ListValue
            Leave ObjectField
            Leave ObjectValue
            Leave Argument
            Leave PathSegment
            Leave Path
            """);
    }

    [Fact]
    public void VisitChildren_EnterReturnsBreakOnFirstListItem_StopsTraversal()
    {
        // arrange
        // breaking on the first int value stops before the remaining list items are entered.
        var node = new FieldSelectionMapParser("f(a: [1, 2, 3])").Parse();
        var visitor = new BreakingVisitor(FieldSelectionMapSyntaxKind.IntValue);

        // act
        visitor.Visit(node, null);

        // assert
        visitor.VisitLog.MatchInlineSnapshot(
            """
            Enter Path
            Enter PathSegment
            Enter Name
            Leave Name
            Enter Argument
            Enter Name
            Leave Name
            Enter ListValue
            Enter IntValue
            """);
    }

    private sealed class CollectingVisitor() : FieldSelectionMapSyntaxVisitor<object?>(Continue)
    {
        private readonly List<string> _log = [];

        public List<FieldSelectionMapSyntaxKind> EnteredKinds { get; } = [];

        public string VisitLog => string.Join(Environment.NewLine, _log);

        protected override ISyntaxVisitorAction Enter(
            IFieldSelectionMapSyntaxNode node,
            object? context)
        {
            EnteredKinds.Add(node.Kind);
            _log.Add($"Enter {node.Kind}");

            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            IFieldSelectionMapSyntaxNode node,
            object? context)
        {
            _log.Add($"Leave {node.Kind}");

            return base.Leave(node, context);
        }
    }

    private sealed class BreakingVisitor(FieldSelectionMapSyntaxKind breakOn)
        : FieldSelectionMapSyntaxVisitor<object?>(Continue)
    {
        private readonly List<string> _log = [];

        public string VisitLog => string.Join(Environment.NewLine, _log);

        protected override ISyntaxVisitorAction Enter(
            IFieldSelectionMapSyntaxNode node,
            object? context)
        {
            _log.Add($"Enter {node.Kind}");

            if (node.Kind == breakOn)
            {
                return Break;
            }

            return base.Enter(node, context);
        }

        protected override ISyntaxVisitorAction Leave(
            IFieldSelectionMapSyntaxNode node,
            object? context)
        {
            _log.Add($"Leave {node.Kind}");

            return base.Leave(node, context);
        }
    }
}
