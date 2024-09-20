using HotChocolate.Language;

namespace StrawberryShake.CodeGeneration.Utilities;

internal sealed class ExtractOperationContext
{
    private readonly DocumentNode _document;
    private int _index = -1;

    public ExtractOperationContext(DocumentNode document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));

        if (!SelectNext())
        {
            throw new ArgumentException("No operation found!", nameof(document));
        }

        AllFragments = _document.Definitions
            .OfType<FragmentDefinitionNode>()
            .ToDictionary(t => t.Name.Value);
    }

    public OperationDefinitionNode Operation { get; private set; } = default!;

    public List<FragmentDefinitionNode> ExportedFragments { get; } = [];

    public Dictionary<string, FragmentDefinitionNode> AllFragments { get; }

    public HashSet<string> VisitedFragments { get; } = [];

    public bool Next()
    {
        ExportedFragments.Clear();
        VisitedFragments.Clear();
        return SelectNext();
    }

    private bool SelectNext()
    {
        for (var i = _index + 1; i < _document.Definitions.Count; i++)
        {
            if (_document.Definitions[i] is OperationDefinitionNode op)
            {
                Operation = op;
                _index = i;
                return true;
            }
        }

        _index = -1;
        return false;
    }
}
