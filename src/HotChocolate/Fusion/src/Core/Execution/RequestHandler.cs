using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal sealed class RequestHandler
{
    private readonly string _schemaName;
    private readonly DocumentNode _document;
    private readonly IReadOnlyList<string> _requires;


    internal RequestHandler(
        string schemaName,
        DocumentNode document,
        IReadOnlyList<string> requires)
    {
        _schemaName = schemaName;
        _document = document;
        _requires = requires;
    }

    public DocumentNode Document => _document;

    private static ReadOnlySpan<byte> Data => new[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' };

    public Request CreateRequest(IExecutionState executionState)
    {
        ObjectValueNode? variables = null;

        if (_requires.Count > 0)
        {

        }

        return new Request(_schemaName, _document, variables, null);
    }
}
