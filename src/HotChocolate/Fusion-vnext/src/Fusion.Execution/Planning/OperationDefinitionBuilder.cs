using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A helper to build an operation definition.
/// </summary>
internal sealed class OperationDefinitionBuilder
{
    private OperationType _type = OperationType.Query;
    private string? _name;
    private string? _description;
    private Lookup? _lookup;
    private string? _requirementKey;
    private SelectionSetNode? _selectionSet;

    private OperationDefinitionBuilder()
    {
    }

    public static OperationDefinitionBuilder New()
        => new();

    public OperationDefinitionBuilder SetType(OperationType type)
    {
        _type = type;
        return this;
    }

    public OperationDefinitionBuilder SetName(string? name)
    {
        _name = name;
        return this;
    }

    public OperationDefinitionBuilder SetDescription(string? description)
    {
        _description = description;
        return this;
    }

    public OperationDefinitionBuilder SetLookup(Lookup? lookup, string? requirementKey)
    {
        _lookup = lookup;
        _requirementKey = requirementKey;
        return this;
    }

    public OperationDefinitionBuilder SetSelectionSet(SelectionSetNode selectionSet)
    {
        _selectionSet = selectionSet;
        return this;
    }

    public (OperationDefinitionNode, ISelectionSetIndex) Build(ISelectionSetIndex index)
    {
        if (_selectionSet is null)
        {
            throw new InvalidOperationException("The operation selection set must be specified.");
        }

        var selectionSet = _selectionSet;

        if (_lookup is not null)
        {
            var arguments = new List<ArgumentNode>();

            foreach (var argument in _lookup.Arguments)
            {
                arguments.Add(
                    new ArgumentNode(
                        new NameNode(argument.Name),
                        new VariableNode(new NameNode($"{_requirementKey}_{argument.Name}"))));
            }

            var lookupField = new FieldNode(
                new NameNode(_lookup.Name),
                null,
                [],
                arguments,
                selectionSet);

            selectionSet = new SelectionSetNode(null, [lookupField]);

            var indexBuilder = index.ToBuilder();
            indexBuilder.Register(selectionSet);
            index = indexBuilder;
        }

        var definition = new OperationDefinitionNode(
            null,
            string.IsNullOrEmpty(_name) ? null : new NameNode(_name),
            string.IsNullOrEmpty(_description) ? null : new StringValueNode(_description),
            _type,
            [],
            [],
            selectionSet);

        return (definition, index);
    }
}
