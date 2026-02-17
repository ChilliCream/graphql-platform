using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// A helper to build an operation definition.
/// </summary>
internal sealed class OperationDefinitionBuilder
{
    private OperationType _type = OperationType.Query;
    private string? _name;
    private Lookup? _lookup;
    private List<ArgumentNode>? _lookupArguments;
    private ITypeDefinition? _typeToLookup;
    private string? _lookupAlias;
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

    public OperationDefinitionBuilder SetLookup(
        Lookup lookup,
        List<ArgumentNode> arguments,
        ITypeDefinition typeToLookup,
        string? alias = null)
    {
        _lookup = lookup;
        _lookupArguments = arguments;
        _typeToLookup = typeToLookup;
        _lookupAlias = alias;
        return this;
    }

    public OperationDefinitionBuilder SetSelectionSet(SelectionSetNode selectionSet)
    {
        _selectionSet = selectionSet;
        return this;
    }

    public (OperationDefinitionNode, ISelectionSetIndex, SelectionPath) Build(ISelectionSetIndex index)
    {
        if (_selectionSet is null)
        {
            throw new InvalidOperationException("The operation selection set must be specified.");
        }

        var selectionSet = _selectionSet;
        var selectionPathBuilder = SelectionPath.CreateBuilder();
        var indexBuilder = index.ToBuilder();

        if (_lookup is not null && _lookupArguments is not null && _typeToLookup is not null)
        {
            if (!_lookup.Path.IsEmpty)
            {
                foreach (var fieldName in _lookup.Path)
                {
                    selectionPathBuilder.AppendField(fieldName);
                }
            }

            selectionPathBuilder.AppendField(_lookup.FieldName);

            var lookupSelectionSet = selectionSet;
            if (_typeToLookup != _lookup.FieldType
                && !selectionSet.Selections.All(s => s is InlineFragmentNode inlineFragment
                    && inlineFragment.TypeCondition?.Name.Value == _typeToLookup.Name))
            {
                var typeRefinement =
                    new InlineFragmentNode(
                        null,
                        new NamedTypeNode(_typeToLookup.Name),
                        [],
                        selectionSet);

                lookupSelectionSet = new SelectionSetNode([
                    new FieldNode(IntrospectionFieldNames.TypeName),
                    typeRefinement
                ]);

                indexBuilder.Register(lookupSelectionSet);

                selectionPathBuilder.AppendFragment(_typeToLookup.Name);
            }

            var alias = _lookupAlias is not null && _lookupAlias != _lookup.FieldName
                ? new NameNode(_lookupAlias)
                : null;

            var lookupField = new FieldNode(
                new NameNode(_lookup.FieldName),
                alias,
                [],
                _lookupArguments,
                lookupSelectionSet);

            if (!_lookup.Path.IsEmpty)
            {
                for (var i = _lookup.Path.Length - 1; i >= 0; i--)
                {
                    var fieldName = _lookup.Path[i];
                    var fieldSelectionSet = new SelectionSetNode(null, [lookupField]);
                    lookupField = new FieldNode(fieldName, fieldSelectionSet);
                    indexBuilder.Register(fieldSelectionSet);
                }
            }

            selectionSet = new SelectionSetNode(null, [lookupField]);
        }

        indexBuilder.Register(selectionSet);
        index = indexBuilder;

        var definition = new OperationDefinitionNode(
            null,
            string.IsNullOrEmpty(_name) ? null : new NameNode(_name),
            null,
            _type,
            [],
            [],
            selectionSet);

        return (definition, index, selectionPathBuilder.Build());
    }
}
