using HotChocolate.Buffers;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Transport.Sockets.Client.Protocols.GraphQLOverWebSocket.Messages;

internal abstract class FusionDataMessage(
    string id,
    PooledSocketPayload payload) : IDataMessage
{
    private SourceResultDocument? _document;
    private int _claimed;
    private bool _documentTransferred;

    public string Id { get; } = id;

    public abstract string Type { get; }

    public int PayloadLength => payload.Length;

    public bool IsClaimed => Volatile.Read(ref _claimed) == 1;

    public bool TryClaim(string id)
        => string.Equals(Id, id, StringComparison.Ordinal)
            && Interlocked.CompareExchange(ref _claimed, 1, 0) == 0;

    public void ParsePayload(IMemoryArenaSource arenaSource)
        => _document ??= WebSocketMessageParser.ParsePayload(
            payload,
            arenaSource.GetNextArena());

    public SourceResultDocument TakePayload()
    {
        var document = _document
            ?? throw ThrowHelper.PayloadNotParsed();
        _documentTransferred = true;
        return document;
    }

    public void Dispose()
    {
        if (!_documentTransferred)
        {
            _document?.Dispose();
        }

        payload.Dispose();
    }
}
