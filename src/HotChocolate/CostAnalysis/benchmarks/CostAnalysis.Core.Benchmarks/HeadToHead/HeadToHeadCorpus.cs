using System.Text.Json;

namespace HotChocolate.CostAnalysis;

internal sealed class HeadToHeadCorpus
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public required string SourceCommit { get; init; }

    public required int DefaultListSize { get; init; }

    public required HeadToHeadScenario[] Scenarios { get; init; }

    public static HeadToHeadCorpus Load(string path)
        => JsonSerializer.Deserialize<HeadToHeadCorpus>(File.ReadAllText(path), s_jsonOptions)
            ?? HeadToHeadThrowHelper.CorpusCouldNotBeRead(path);
}

internal sealed class HeadToHeadScenario
{
    public required string Id { get; init; }

    public required string Axis { get; init; }

    public required int ObjectTypes { get; init; }

    public required int AbstractTypes { get; init; }

    public required int IncidencesPerObject { get; init; }

    public required int QuerySpreads { get; init; }

    public int? BooleanVariablesPerRegion { get; init; }

    public required string Schema { get; init; }

    public required string Operation { get; init; }

    public required JsonElement Variables { get; init; }

    public required double ExpectedTypeCost { get; init; }

    public required double ExpectedFieldCost { get; init; }
}
