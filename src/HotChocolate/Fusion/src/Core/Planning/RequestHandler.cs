using System.Text.Json;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Fusion;

internal sealed class RequestHandler
{
    private readonly DocumentNode _document;

    internal RequestHandler(DocumentNode document)
    {
        _document = document;
    }

    public Request CreateRequest(IReadOnlyList<IValueNode>? variables)
        => throw new NotImplementedException();

    public IReadOnlyList<IValueNode> ExtractState(JsonElement response)
        => throw new NotImplementedException();

    public void ExtractResult(JsonElement response, ObjectResult parent)
        => throw new NotImplementedException();
}
