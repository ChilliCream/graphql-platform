using System.Text;
using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

public class FieldGroupMergerTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(9)]
    [InlineData(12)]
    public void Build_Should_MatchMerger_When_VisitedOrderIsReversedAndFieldsAreNested(
        int groupCount)
    {
        // arrange
        var schemaSource = new StringBuilder("type Query {");
        var operationSource = new StringBuilder("query($include: Boolean!) {");

        for (var i = 0; i < groupCount; i++)
        {
            schemaSource.Append(" f").Append(i).Append(": Child");
            operationSource.Append(" r").Append(i).Append(": f").Append(i).Append(" { value }");
        }

        schemaSource.Append(" } type Child { value: Int }");
        operationSource.Append(" ... @include(if: $include) {");

        for (var i = 0; i < groupCount; i++)
        {
            operationSource.Append(" r").Append(i).Append(": f")
                .Append((i + 1) % groupCount).Append(" { value }");
        }

        operationSource.Append(" } }");
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(schemaSource.ToString());
        var document = Utf8GraphQLParser.Parse(operationSource.ToString());
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            "Query");
        var conditionalNodeId = Enumerable.Range(0, tree.Nodes.Count)
            .Single(i => tree.Nodes[i].Condition.BooleanCondition.Length > 0);
        int[] visited = [conditionalNodeId, tree.RootNodeId];
        var expected = FieldGroupMerger.Merge(tree, visited);
        var scratch = new int[FieldGroupAccumulator.GetRequiredScratchLength(tree)];
        var accumulator = new FieldGroupAccumulator(tree, scratch);

        // act
        accumulator.Build(visited);
        var actual = new List<(string ResponseName, FieldNode[] Fields, bool HasChildSelections)>();

        for (var i = 0; i < accumulator.Count; i++)
        {
            var responseNameId = accumulator.GetResponseNameId(i);
            var firstGroup = accumulator.GetGroup(accumulator.GetFirstEntry(responseNameId));
            actual.Add((
                firstGroup.ResponseName,
                accumulator.MaterializeFields(responseNameId),
                accumulator.HasChildSelections(responseNameId)));
        }

        // assert
        Assert.Equal(expected.Select(t => t.ResponseName), actual.Select(t => t.ResponseName));
        Assert.All(
            expected.Zip(actual),
            pair => Assert.True(
                pair.First.Fields.Zip(pair.Second.Fields, ReferenceEquals)
                    .All(static match => match)));
        Assert.Equal(
            expected.Select(t => FieldGroupMerger.MergedSelections(t.Fields).Count > 0),
            actual.Select(t => t.HasChildSelections));
    }

    [Theory]
    [InlineData("query { a: f0 b: f1 }", true)]
    [InlineData("query($include: Boolean!) { a: f0 a: f1 @include(if: $include) }", false)]
    public void ExtractOperation_Should_ReportUniqueResponseNames_When_ResponseNamesVary(
        string operationSource,
        bool expected)
    {
        // arrange
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot("type Query { f0: Int f1: Int }");
        var document = Utf8GraphQLParser.Parse(operationSource);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);

        // act
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            "Query");

        // assert
        Assert.Equal(expected, tree.HasUniqueResponseNames);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(9)]
    public void Merge_Should_PreserveOrderAndDuplicates_When_CrossingIndexThreshold(
        int groupCount)
    {
        // arrange
        var schemaSource = new StringBuilder("type Query {");
        var operationSource = new StringBuilder("query($include: Boolean!) {");

        for (var i = 0; i < groupCount; i++)
        {
            schemaSource.Append(" f").Append(i).Append(": Int");
            operationSource.Append(" r").Append(i).Append(": f").Append(i);
        }

        schemaSource.Append(" }");
        operationSource.Append(" r0: f1 @include(if: $include) }");
        var snapshot = ConditionTreeTestHelpers.BuildSnapshot(schemaSource.ToString());
        var document = Utf8GraphQLParser.Parse(operationSource.ToString());
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var tree = ConditionTreeExtractor.ExtractOperation(
            snapshot,
            document,
            operation,
            "Query");
        var conditionalNodeId = Enumerable.Range(0, tree.Nodes.Count)
            .Single(i => tree.Nodes[i].Condition.BooleanCondition.Length > 0);

        // act
        var groups = FieldGroupMerger.Merge(tree, [tree.RootNodeId, conditionalNodeId]);

        // assert
        Assert.Equal(
            Enumerable.Range(0, groupCount).Select(i => $"r{i}"),
            groups.Select(t => t.ResponseName));
        Assert.Equal(["f0", "f1"], groups[0].Fields.Select(t => t.Name.Value));
        Assert.Equal(
            Enumerable.Range(1, groupCount - 1).Select(i => $"f{i}"),
            groups.Skip(1).SelectMany(t => t.Fields).Select(t => t.Name.Value));
    }
}
