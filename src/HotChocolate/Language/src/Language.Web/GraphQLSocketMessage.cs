namespace HotChocolate.Language;

public readonly ref struct GraphQLSocketMessage
{
    public GraphQLSocketMessage(string type, string? id, ReadOnlySpan<byte> payload)
    {
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Id = id;
        Payload = payload;
    }

    public string? Id { get; }

    public string Type { get; }

    public ReadOnlySpan<byte> Payload { get; }
}
