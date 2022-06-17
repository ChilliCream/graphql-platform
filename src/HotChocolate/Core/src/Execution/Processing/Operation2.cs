using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class Operation2 : IPreparedOperation2
{
    private readonly SelectionVariants2[] _selectionVariants;
    private readonly IncludeCondition[] _includeConditions;

    public Operation2(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        ObjectType rootType,
        SelectionVariants2[] selectionVariants,
        IncludeCondition[] includeConditions)
    {
        Id = id;
        Document = document;
        Definition = definition;
        RootType = rootType;
        Type = definition.Operation;

        if (definition.Name?.Value is { } name)
        {
            Name = name;
        }

        SelectionVariants2 root = selectionVariants[0];
        RootSelectionSet = root.GetSelectionSet(rootType);
        _selectionVariants = selectionVariants;
        _includeConditions = includeConditions;
    }

    public string Id { get; }

    public DocumentNode Document { get; }

    public OperationDefinitionNode Definition { get; }

    public ObjectType RootType { get; }

    public NameString? Name { get; }

    public OperationType Type { get; }

    public ISelectionSet2 RootSelectionSet { get; }

    public IReadOnlyList<ISelectionVariants2> SelectionVariants
        => _selectionVariants;

    public IReadOnlyList<IncludeCondition> IncludeConditions
        => _includeConditions;

    public ISelectionSet2 GetSelectionSet(ISelection2 selection, IObjectType typeContext)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        if (typeContext is null)
        {
            throw new ArgumentNullException(nameof(typeContext));
        }

        var selectionSetId = ((Selection2)selection).SelectionSetId;

        if (selectionSetId == -1)
        {
            throw new ArgumentException("The specified selection does not have a selection set.");
        }

        return _selectionVariants[selectionSetId].GetSelectionSet(typeContext);
    }

    public IEnumerable<IObjectType> GetPossibleTypes(ISelection2 selection)
    {
        if (selection is null)
        {
            throw new ArgumentNullException(nameof(selection));
        }

        var selectionSetId = ((Selection2)selection).SelectionSetId;

        if (selectionSetId == -1)
        {
            throw new ArgumentException("The specified selection does not have a selection set.");
        }

        return _selectionVariants[selectionSetId].GetPossibleTypes();
    }

    public long CreateIncludeContext(IVariableValueCollection variables)
    {
        long context = 0;

        for (var i = 0; i < _includeConditions.Length; i++)
        {
            if (_includeConditions[i].IsIncluded(variables))
            {
                long flag = 2 ^ i;
                context |= flag;
            }
        }

        return context;
    }

    public string Print() => OperationPrinter.Print(this);

    public override string ToString() => Print();
}
