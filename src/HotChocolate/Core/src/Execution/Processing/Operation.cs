using System;
using System.Collections;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Execution.Properties.Resources;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed class Operation : IOperation
{
    private readonly SelectionVariants[] _selectionVariants;
    private readonly IncludeCondition[] _includeConditions;

    public Operation(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        ObjectType rootType,
        SelectionVariants[] selectionVariants,
        IncludeCondition[] includeConditions,
        Dictionary<string, object?> contextData,
        bool hasIncrementalParts)
    {
        Id = id;
        Document = document;
        Definition = definition;
        RootType = rootType;
        ContextData = contextData;
        Type = definition.Operation;

        if (definition.Name?.Value is { } name)
        {
            Name = name;
        }

        var root = selectionVariants[0];
        RootSelectionSet = root.GetSelectionSet(rootType);
        _selectionVariants = selectionVariants;
        HasIncrementalParts = hasIncrementalParts;
        _includeConditions = includeConditions;
    }

    public string Id { get; }

    public DocumentNode Document { get; }

    public OperationDefinitionNode Definition { get; }

    public ObjectType RootType { get; }

    public string? Name { get; }

    public OperationType Type { get; }

    public ISelectionSet RootSelectionSet { get; }

    public IReadOnlyList<ISelectionVariants> SelectionVariants
        => _selectionVariants;

    public bool HasIncrementalParts { get; }

    public IReadOnlyList<IncludeCondition> IncludeConditions
        => _includeConditions;

    public IReadOnlyDictionary<string, object?> ContextData { get; }

    public ISelectionSet GetSelectionSet(ISelection selection, IObjectType typeContext)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        if (typeContext is null)
        {
            throw new ArgumentNullException(nameof(typeContext));
        }

        var selectionSetId = ((Selection)selection).SelectionSetId;

        if (selectionSetId is -1)
        {
            throw Operation_NoSelectionSet();
        }

        return _selectionVariants[selectionSetId].GetSelectionSet(typeContext);
    }

    public IEnumerable<IObjectType> GetPossibleTypes(ISelection selection)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        var selectionSetId = ((Selection)selection).SelectionSetId;

        if (selectionSetId == -1)
        {
            throw new ArgumentException(Operation_GetPossibleTypes_NoSelectionSet);
        }

        return _selectionVariants[selectionSetId].GetPossibleTypes();
    }

    public long CreateIncludeFlags(IVariableValueCollection variables)
    {
        long context = 0;

        for (var i = 0; i < _includeConditions.Length; i++)
        {
            if (_includeConditions[i].IsIncluded(variables))
            {
                long flag = 1;
                flag <<= i;
                context |= flag;
            }
        }

        return context;
    }

    public IEnumerator<ISelectionSet> GetEnumerator()
    {
        foreach (var selectionVariant in _selectionVariants)
        {
            foreach (var objectType in selectionVariant.GetPossibleTypes())
            {
                yield return selectionVariant.GetSelectionSet(objectType);
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public override string ToString() => OperationPrinter.Print(this);
}
