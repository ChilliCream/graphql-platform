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
    internal SelectedField(IResolverContext resolverContext, ISelection selection)
    {
        _resolverContext = resolverContext;
        Selection = selection;
    }

    /// <inheritdoc />
    public ISelection Selection { get; }

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
        var fields = GetFieldSelections(type, allowInternals);

        if (fields is null)
        {
            return [];
        }

        var finalFields = new SelectedField[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            finalFields[i] = new SelectedField(_resolverContext, fields[i]);
        }

        return finalFields;
    }

    /// <inheritdoc />
    public bool IsSelected(
        string fieldName,
        ObjectType? type = null,
        bool allowInternals = false)
    {
        var fields = GetFieldSelections(type, allowInternals);

        if (fields is null)
        {
            return false;
        }

        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i].Field.Name == fieldName)
            {
                return true;
            }
        }

        return false;
    }

    private IReadOnlyList<ISelection>? GetFieldSelections(
        ObjectType? type = null,
        bool allowInternals = false)
    {
        var namedType = Field.Type.NamedType();

        if (Selection.SelectionSet is null)
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
