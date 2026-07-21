using System.Collections.Concurrent;
using System.Xml.Linq;
using GreenDonut;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types;

public class DataLoaderTests
{
    [Fact]
    public void GeneratedDataLoaders_Should_ReferenceSourceMethod_When_XmlDocumentationIsGenerated()
    {
        // arrange
        var documentationPath = global::System.IO.Path.ChangeExtension(
            typeof(DataLoaderTests).Assembly.Location,
            ".xml");
        var document = XDocument.Load(documentationPath);
        var generatedTypeNames = new[]
        {
            "T:HotChocolate.Types.IValueByKeyDataLoader",
            "T:HotChocolate.Types.ValueByKeyDataLoader"
        };

        // act
        var documentation = document
            .Descendants("member")
            .Where(m => generatedTypeNames.Contains(m.Attribute("name")?.Value))
            .OrderBy(m => m.Attribute("name")?.Value, StringComparer.Ordinal)
            .Select(m =>
            {
                var summary = m.Element("summary")!;
                return $"""
                    {m.Attribute("name")!.Value}
                    Summary: {summary.Value.Trim()}
                    Cref: {summary.Element("see")!.Attribute("cref")!.Value}
                    """;
            });

        // assert
        string.Join(Environment.NewLine, documentation).MatchInlineSnapshot(
            """
            T:HotChocolate.Types.IValueByKeyDataLoader
            Summary: A DataLoader generated from .
            Cref: M:HotChocolate.Types.DataLoaders.GetValueByKeyAsync(System.Collections.Generic.IReadOnlyList{System.Int32},System.Threading.CancellationToken)
            T:HotChocolate.Types.ValueByKeyDataLoader
            Summary: A DataLoader generated from .
            Cref: M:HotChocolate.Types.DataLoaders.GetValueByKeyAsync(System.Collections.Generic.IReadOnlyList{System.Int32},System.Threading.CancellationToken)
            """);
    }

    [Fact]
    public async Task DataLoader_Should_Split_Keys_Into_Batches_When_MaxBatchSize_Is_Set()
    {
        // arrange
        DataLoaders.RecordedBatchSizes.Clear();

        var dataLoader = new ValueByKeyDataLoader(
            new ServiceCollection().BuildServiceProvider(),
            AutoBatchScheduler.Default,
            new DataLoaderOptions());

        // act
        var result = await dataLoader.LoadAsync(
            new[] { 1, 2, 3, 4, 5 },
            TestContext.Current.CancellationToken);

        // assert
        // MaxBatchSize = 2 splits the five keys into batches of sizes 2, 2 and 1.
        Assert.Equal(new[] { 1, 2, 2 }, DataLoaders.RecordedBatchSizes.OrderBy(x => x));
        Assert.Equal(new[] { "1", "2", "3", "4", "5" }, result);
    }
}

public static class DataLoaders
{
    public static readonly ConcurrentQueue<int> RecordedBatchSizes = new();

    [DataLoader(MaxBatchSize = 2)]
    public static Task<IReadOnlyDictionary<int, string>> GetValueByKeyAsync(
        IReadOnlyList<int> keys,
        CancellationToken cancellationToken)
    {
        RecordedBatchSizes.Enqueue(keys.Count);
        IReadOnlyDictionary<int, string> result = keys.ToDictionary(k => k, k => k.ToString());
        return Task.FromResult(result);
    }
}
