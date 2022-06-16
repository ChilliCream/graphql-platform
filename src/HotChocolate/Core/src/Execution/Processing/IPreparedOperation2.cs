using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// A prepared operations is an already compiled and optimized variant
/// of the operation specified in the query document that was provided
/// in the request.
/// </summary>
public interface IPreparedOperation2 : IOperation
{
    /// <summary>
    /// Gets the internal unique identifier for this operation.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the prepared root selections for this operation.
    /// </summary>
    /// <returns>
    /// Returns the prepared root selections for this operation.
    /// </returns>
    ISelectionSet2 RootSelectionSet { get; }

    /// <summary>
    /// Gets all selection variants of this operation.
    /// </summary>
    IReadOnlyList<ISelectionVariants2> SelectionVariants { get; }

    /// <summary>
    /// Gets the list of include conditions associated with this operation.
    /// </summary>
    IReadOnlyList<IncludeCondition> IncludeConditions { get; }

    /// <summary>
    /// Gets the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection set for which the selection set shall be resolved.
    /// </param>
    /// <param name="typeContext">
    /// The result type context.
    /// </param>
    /// <returns>
    /// Returns the selection set for the specified <paramref name="selection"/> and
    /// <paramref name="typeContext"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    ISelectionSet2 GetSelectionSet(ISelection2 selection, IObjectType typeContext);

    /// <summary>
    /// Gets the possible return types for the <paramref name="selection"/>.
    /// </summary>
    /// <param name="selection">
    /// The selection for which the possible result types shall be returned.
    /// </param>
    /// <returns>
    /// Returns the possible return types for the specified <paramref name="selection"/>.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// The specified <paramref name="selection"/> has no selection set.
    /// </exception>
    IEnumerable<IObjectType> GetPossibleTypes(ISelection2 selection);

    long CreateIncludeContext(IVariableValueCollection variables);

    /// <summary>
    /// Prints the prepared operation.
    /// </summary>
    string Print();
}

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

    public string Print()
        => OperationPrinter.
}

internal static class OperationPrinter
{
    private static SelectionSetNode Visit(IPreparedOperation2 operation, ISelectionVariants2 selectionVariants)
    {
        var fragments = new List<InlineFragmentNode>();

        foreach (IObjectType objectType in selectionVariants.GetPossibleTypes())
        {
            var typeContext = (ObjectType)objectType;
            var selections = new List<ISelectionNode>();

            foreach (var selection in selectionVariants.GetSelectionSet(typeContext).Selections)
            {
                var selectionSetId = ((Selection2)selection).SelectionSetId;

                SelectionSetNode? selectionSetNode =
                    selection.SelectionSet is not null
                        ? Visit(operation, operation.SelectionVariants[selectionSetId])
                        : null;
                selections.Add(CreateSelection(selection, selectionSetNode));
            }

            fragments.Add(new InlineFragmentNode(
                null,
                new NamedTypeNode(typeContext.Name),
                Array.Empty<DirectiveNode>(),
                new SelectionSetNode(selections)));
        }

        return new SelectionSetNode(fragments);
    }

    private static FieldNode CreateSelection(ISelection2 selection, SelectionSetNode? selectionSet)
    {
        var directives = new List<DirectiveNode>();

        if (selection.IncludeConditions is not null)
        {
            foreach (SelectionIncludeCondition condition in selection.IncludeConditions)
            {
                if (condition.Skip is not null)
                {
                    directives.Add(
                        new DirectiveNode(
                            "skip",
                            new ArgumentNode("if", condition.Skip)));
                }

                if (condition.Include is not null)
                {
                    directives.Add(
                        new DirectiveNode(
                            "include",
                            new ArgumentNode("if", condition.Include)));
                }
            }
        }

        directives.Add(CreateExecutionInfo(selection));

        return new FieldNode(
            null,
            selection.SyntaxNode.Name,
            selection.SyntaxNode.Alias,
            null,
            directives,
            selection.SyntaxNode.Arguments,
            selectionSet);
    }

    private static DirectiveNode CreateExecutionInfo(ISelection selection)
    {
        var arguments = new ArgumentNode[selection.IsInternal ? 4 : 3];
        arguments[0] = new ArgumentNode("id", new IntValueNode(selection.Id));
        arguments[1] = new ArgumentNode("kind", new EnumValueNode(selection.Strategy));

        if (selection.Type.IsListType())
        {
            if (selection.Type.NamedType().IsLeafType())
            {
                arguments[2] = new ArgumentNode("type", new EnumValueNode("LEAF_LIST"));
            }
            else
            {
                arguments[2] = new ArgumentNode("type", new EnumValueNode("COMPOSITE_LIST"));
            }
        }
        else if (selection.Type.IsCompositeType())
        {
            arguments[2] = new ArgumentNode("type", new EnumValueNode("COMPOSITE"));
        }
        else if (selection.Type.IsLeafType())
        {
            arguments[2] = new ArgumentNode("type", new EnumValueNode("LEAF"));
        }

        if (selection.IsInternal)
        {
            arguments[3] = new ArgumentNode("internal", BooleanValueNode.True);
        }

        return new DirectiveNode("__execute", arguments);
    }
}
