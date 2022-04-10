using System;
using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

#nullable enable

namespace HotChocolate.Data.Projections.Context;

/// <inheritdoc />
public sealed class SelectedField
    : ISelectedField
{
    private readonly IResolverContext _resolverContext;
    private readonly ISelection _selection;

    /// <summary>
    /// Creates a new instance of <see cref="SelectedField"/>
    /// </summary>
    internal SelectedField(
        IResolverContext resolverContext,
        ISelection selection)
    {
        _resolverContext = resolverContext;
        _selection = selection;
    }

    /// <inheritdoc />
    public IFieldSelection Selection => _selection;

    /// <inheritdoc />
    public IObjectField Field => Selection.Field;

    /// <inheritdoc />
    public IType Type => Selection.Type;

    /// <inheritdoc />
    public bool IsAbstractType => Selection.Type.IsAbstractType();

    /// <inheritdoc />
    public IReadOnlyList<ISelectedField> GetFields(
        ObjectType? type = null,
        bool allowInternals = false)
    {
        IReadOnlyList<IFieldSelection>? fields = GetFieldSelections(type, allowInternals);

        if (fields is null)
        {
            return Array.Empty<SelectedField>();
        }

        List<SelectedField> finalFields = new();

        for (var i = 0; i < fields.Count; i++)
        {
            if (fields[i] is ISelection selection)
            {
                finalFields.Add(new SelectedField(_resolverContext, selection));
            }
        }

        return finalFields;
    }

    /// <inheritdoc />
    public bool IsSelected(
        NameString fieldName,
        ObjectType? type = null,
        bool allowInternals = false)
    {
        IReadOnlyList<IFieldSelection>? fields = GetFieldSelections(type, allowInternals);

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

    private IReadOnlyList<IFieldSelection>? GetFieldSelections(
        ObjectType? type = null,
        bool allowInternals = false)
    {
        INamedType namedType = Field.Type.NamedType();

        SelectionSetNode? selectionSet = _selection.SelectionSet;

        if (selectionSet is null)
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
                IEnumerable<ObjectType> possibleTypes =
                    _resolverContext.Schema.GetPossibleTypes(namedType);

                throw ThrowHelper
                    .SelectionContext_NoTypeForAbstractFieldProvided(namedType, possibleTypes);
            }
        }

        return _resolverContext.GetSelections(type, selectionSet, allowInternals);
    }
}
