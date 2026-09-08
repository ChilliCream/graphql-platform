using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

public class ConditionTreeTests
{
    private const string BookSchema =
        """
        type Query { book: Book }
        type Book { title: String }
        """;

    private static ConditionTree ExtractRoot(string sdl, string operation, IReadOnlyDictionary<string, bool>? known = null)
    {
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(sdl);
        var document = Utf8GraphQLParser.Parse(operation);
        var operationDefinition = ConditionTreeTestHelpers.ParseOperation(document);
        return ConditionTreeExtractor.ExtractOperation(snapshot, document, operationDefinition, "Query", known);
    }

    [Fact]
    public void ExtractOperation_Should_Collect_Field_Into_Root_Node_When_No_Fragments_Or_Directives()
    {
        // arrange
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(BookSchema);
        var document = Utf8GraphQLParser.Parse("{ book { title } }");
        var operation = ConditionTreeTestHelpers.ParseOperation(document);

        // act
        var tree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");

        // assert
        ConditionTreeTestHelpers.Dump(snapshot, tree, "Query").MatchInlineSnapshot(
            """
            *[Query] () book:1
            """);
    }

    [Fact]
    public void ExtractOperation_Should_Split_Nodes_By_Type_Condition_When_Union_Has_Exclusive_Fragments()
    {
        // arrange: c1-exclusive-types shape, extracting the nested boundary under `result`
        const string sdl =
            """
            union Result = A | B
            type A { a: Int }
            type B { b: Int }
            type Query { result: Result }
            """;
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(sdl);
        var document = Utf8GraphQLParser.Parse(
            "{ result { ... on A { a } ... on B { b } } }");
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var rootTree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        var resultGroup = rootTree.Root.FieldGroups.Single(g => g.ResponseName == "result");
        var childRoot = new Condition(snapshot.GetPossibleTypeSet("Result"), []);

        // act: the field's nested selection set is its own boundary
        var childTree = ConditionTreeExtractor.ExtractBoundary(
            snapshot,
            ConditionTreeExtractor.IndexFragments(document),
            resultGroup.MergedSelectionSet(),
            childRoot);

        // assert
        ConditionTreeTestHelpers.Dump(snapshot, childTree, "A", "B").MatchInlineSnapshot(
            """
            *[A,B] ()
             [A] () a:1
             [B] () b:1
            """);
    }

    [Fact]
    public void ExtractOperation_Should_Dedup_Duplicate_Response_Name_Into_One_Group()
    {
        // arrange: c4-duplicate-response-name shape
        const string sdl =
            """
            union Result = A
            type A { a: Int }
            type Query { result: Result }
            """;
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(sdl);
        var document = Utf8GraphQLParser.Parse(
            "{ result { ... on A { label: a } ... on A { label: a } } }");
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var rootTree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        var resultGroup = rootTree.Root.FieldGroups.Single(g => g.ResponseName == "result");
        var childRoot = new Condition(snapshot.GetPossibleTypeSet("Result"), []);

        // act
        var childTree = ConditionTreeExtractor.ExtractBoundary(
            snapshot,
            ConditionTreeExtractor.IndexFragments(document),
            resultGroup.MergedSelectionSet(),
            childRoot);

        // assert: both `... on A` fragments collapse onto one node, and both
        // `label: a` occurrences land in one response-name group
        Assert.Single(childTree.Nodes);
        Assert.Equal(2, childTree.Root.FieldGroups.Single().Fields.Count);
    }

    [Theory]
    [InlineData("{ book @include(if: false) }")]
    [InlineData("{ book @skip(if: true) }")]
    [InlineData("query($x: Boolean!) { book @include(if: $x) @skip(if: $x) }")]
    [InlineData("{ book @include(if: 1) }")]
    public void ExtractOperation_Should_Drop_Selection_When_Infeasible(string operation)
    {
        // arrange & act
        var tree = ExtractRoot(BookSchema, operation);

        // assert: no node anywhere in the arena collected the `book` field
        var groupsNamedBook = tree.Nodes.SelectMany(n => n.FieldGroups).Count(g => g.ResponseName == "book");
        Assert.Equal(0, groupsNamedBook);
    }

    [Fact]
    public void ExtractOperation_Should_Branch_When_Include_Variable_Is_Unresolved()
    {
        // arrange & act
        var tree = ExtractRoot(BookSchema, "query($x: Boolean!) { book @include(if: $x) }");

        // assert
        Assert.Equal(2, tree.Nodes.Count);
        Assert.Empty(tree.Root.FieldGroups);
        Assert.Equal("+x", tree.Nodes[1].Condition.BooleanCondition.Single().ToString());
        Assert.Equal("book", tree.Nodes[1].FieldGroups.Single().ResponseName);
    }

    [Fact]
    public void ExtractOperation_Should_Prune_Selection_When_Known_Variable_Makes_Include_Inactive()
    {
        // arrange & act
        var tree = ExtractRoot(
            BookSchema,
            "query($x: Boolean!) { book @include(if: $x) }",
            new Dictionary<string, bool> { ["x"] = false });

        // assert: pruned before any branch is even materialized
        Assert.Single(tree.Nodes);
        Assert.Empty(tree.Root.FieldGroups);
    }

    [Fact]
    public void ExtractOperation_Should_Keep_Branch_When_Known_Variable_Makes_Include_Active()
    {
        // arrange & act: known-active edges stay edges, they are not collapsed into the root
        var tree = ExtractRoot(
            BookSchema,
            "query($x: Boolean!) { book @include(if: $x) }",
            new Dictionary<string, bool> { ["x"] = true });

        // assert
        Assert.Equal(2, tree.Nodes.Count);
        Assert.Empty(tree.Root.FieldGroups);
        Assert.Equal("book", tree.Nodes[1].FieldGroups.Single().ResponseName);
    }
}
