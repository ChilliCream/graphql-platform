using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Tests that equivalent named and inline fragments produce identical condition trees.
/// </summary>
public class DocumentShapeEquivalenceTests
{
    private const string ExclusiveTypesSdl =
        """
        union Result = A | B
        type A { a: Int }
        type B { b: Int }
        type Query { result: Result }
        """;

    private const string ExclusiveTypesInline =
        "{ result { ... on A { a } ... on B { b } } }";

    private const string ExclusiveTypesFragments =
        """
        { result { ...OnA ...OnB } }
        fragment OnA on A { a }
        fragment OnB on B { b }
        """;

    private const string DuplicateResponseNameSdl =
        """
        union Result = A
        type A { a: Int }
        type Query { result: Result }
        """;

    private const string DuplicateResponseNameInline =
        "{ result { ... on A { label: a } ... on A { label: a } } }";

    private const string DuplicateResponseNameFragments =
        """
        { result { ...OnA ...OnA } }
        fragment OnA on A { label: a }
        """;

    [Fact]
    public void ExtractOperation_Should_Produce_Identical_Tree_For_Inline_And_Named_Fragments_When_C1_Exclusive_Types()
    {
        // arrange & act
        var inline = ExtractResultBoundary(ExclusiveTypesSdl, ExclusiveTypesInline, "Result", "A", "B");
        var named = ExtractResultBoundary(ExclusiveTypesSdl, ExclusiveTypesFragments, "Result", "A", "B");

        // assert
        Assert.Equal(inline, named);
    }

    [Fact]
    public void ExtractOperation_Should_Produce_Identical_Tree_When_C4_Duplicate_Response_Name()
    {
        // arrange & act
        var inline = ExtractResultBoundary(DuplicateResponseNameSdl, DuplicateResponseNameInline, "Result", "A");
        var named = ExtractResultBoundary(DuplicateResponseNameSdl, DuplicateResponseNameFragments, "Result", "A");

        // assert
        Assert.Equal(inline, named);
    }

    /// <summary>
    /// Returns text representations of the operation's root tree and its single result field's child tree.
    /// </summary>
    private static string ExtractResultBoundary(
        string sdl,
        string operationText,
        string resultTypeName,
        params string[] objectTypeNames)
    {
        var schemaIndex = ConditionTreeTestHelpers.BuildSchemaIndex(sdl);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);

        var rootTree = ConditionTreeExtractor.ExtractOperation(schemaIndex, document, operation, "Query");
        var resultGroup = rootTree.Root.FieldGroups.Single(g => g.ResponseName == "result");

        var childRoot = new Condition(schemaIndex.GetPossibleTypeSet(resultTypeName), []);
        var childTree = ConditionTreeExtractor.ExtractBoundary(
            schemaIndex,
            fragments,
            resultGroup.MergedSelectionSet(),
            childRoot);

        return ConditionTreeTestHelpers.Dump(schemaIndex, rootTree, "Query")
            + "---"
            + ConditionTreeTestHelpers.Dump(schemaIndex, childTree, objectTypeNames);
    }
}
