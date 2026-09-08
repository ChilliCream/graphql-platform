using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Pins that the raw fixture spelling (named fragments) and an equivalent
/// inline-fragment spelling of the same operation extract identical
/// condition trees, mirroring the conformance suite's c1/c4 fixture pairs.
/// </summary>
public class DocumentShapeEquivalenceTests
{
    // c1-exclusive-types / c1-exclusive-types-fragments: same SDL, same
    // expected 2/21, one spelled with inline fragments, one with named ones.
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

    // c4-duplicate-response-name / c4-duplicate-response-name-fragments:
    // same SDL, same expected 2/11.
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
    public void ExtractOperation_Should_Produce_Identical_Tree_For_Inline_And_Named_Fragments_When_C4_Duplicate_Response_Name()
    {
        // arrange & act
        var inline = ExtractResultBoundary(DuplicateResponseNameSdl, DuplicateResponseNameInline, "Result", "A");
        var named = ExtractResultBoundary(DuplicateResponseNameSdl, DuplicateResponseNameFragments, "Result", "A");

        // assert
        Assert.Equal(inline, named);
    }

    /// <summary>
    /// Extracts the root boundary and, from its single <c>result</c> field
    /// group, the nested boundary under <paramref name="resultTypeName"/>,
    /// then dumps both so a spelling-invariant comparison can assert on the
    /// combined text.
    /// </summary>
    private static string ExtractResultBoundary(
        string sdl,
        string operationText,
        string resultTypeName,
        params string[] objectTypeNames)
    {
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(sdl);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);

        var rootTree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        var resultGroup = rootTree.Root.FieldGroups.Single(g => g.ResponseName == "result");

        var childRoot = new Condition(snapshot.GetPossibleTypeSet(resultTypeName), []);
        var childTree = ConditionTreeExtractor.ExtractBoundary(
            snapshot,
            fragments,
            resultGroup.MergedSelectionSet(),
            childRoot);

        return ConditionTreeTestHelpers.Dump(snapshot, rootTree, "Query")
            + "---"
            + ConditionTreeTestHelpers.Dump(snapshot, childTree, objectTypeNames);
    }
}
