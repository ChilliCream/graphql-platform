using HotChocolate.Execution.Processing;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using static HotChocolate.Data.ThrowHelper;

namespace HotChocolate.Data.Projections.Context;

/// <inheritdoc />
public sealed class SelectedField : ISelectedField
{
    private readonly IResolverContext _resolverContext;

    /// <summary>
    /// Creates a new instance of <see cref="SelectedField"/>
    /// </summary>
    internal SelectedField(IResolverContext resolverContext, Selection selection)
    {
        _resolverContext = resolverContext;
        Selection = selection;
    }

    /// <inheritdoc />
    public Selection Selection { get; }

    /// <inheritdoc />
    public IOutputFieldDefinition Field => Selection.Field;

    /// <inheritdoc />
    public IType Type => Selection.Type;

    /// <inheritdoc />
    public bool IsAbstractType => Selection.Type.IsAbstractType();

    /// <inheritdoc />
    public IReadOnlyList<ISelectedField> GetFields(
        ObjectType? type = null,
        bool allowInternals = false)
    {
        var selections = GetSelections(type, allowInternals);

        if (selections is null)
        {
            return [];
        }

        var selectedFields = new List<SelectedField>();

        foreach (var selection in selections)
        {
            selectedFields.Add(new SelectedField(_resolverContext, selection));
        }

        return selectedFields;
    }

    /// <inheritdoc />
    public bool IsSelected(
        string fieldName,
        ObjectType? type = null,
        bool allowInternals = false)
    {
        var selections = GetSelections(type, allowInternals);

        if (selections is null)
        {
            return false;
        }

        foreach (var selection in selections)
        {
            if (selection.Field.Name == fieldName)
            {
                return true;
            }
        }

        return false;
    }

    private SelectionEnumerator? GetSelections(
        ObjectType? type = null,
        bool allowInternals = false)
    {
        var namedType = Field.Type.NamedType();

        if (Selection.IsLeaf)
        {
            return null;
        }

        if (type is null)
        {
            if (namedType is ObjectType objectType)
            {
                type = objectType;
            }
            else
            {
                var possibleTypes = _resolverContext.Schema.GetPossibleTypes(namedType);
                throw SelectionContext_NoTypeForAbstractFieldProvided(namedType, possibleTypes);
            }
        }

        return _resolverContext.GetSelections(type, Selection, allowInternals);
    }
}
