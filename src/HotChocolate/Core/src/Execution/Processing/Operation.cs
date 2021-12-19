using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

internal sealed class Operation : IPreparedOperation
{
    private readonly IReadOnlyDictionary<SelectionSetNode, SelectionVariants> _selectionSets;

    public Operation(
        string id,
        DocumentNode document,
        OperationDefinitionNode definition,
        ObjectType rootType,
        Dictionary<SelectionSetNode, SelectionVariants> selectionSets)
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

        // we ensure that there is at least one selection set to be executed even if
        // all roots were deferred.
        SelectionVariants rootSelectionVariants = selectionSets[definition.SelectionSet];
        ISelectionSet selectionSet = rootSelectionVariants.GetSelectionSet(rootType);

        if (selectionSet.Selections.Count == 0 && selectionSet.Fragments.Count > 0)
        {
            var fragments = selectionSet.Fragments.ToList();
            selectionSet = fragments[0].SelectionSet;
            fragments.RemoveAt(0);
            fragments.AddRange(selectionSet.Fragments);
            selectionSet = new SelectionSet(
                selectionSet.Selections,
                fragments,
                selectionSet.IsConditional);
            rootSelectionVariants.ReplaceSelectionSet(rootType, selectionSet);
        }

        RootSelectionVariants = rootSelectionVariants;
        _selectionSets = selectionSets;
    }

    public string Id { get; }

    public NameString? Name { get; }

    public DocumentNode Document { get; }

    public OperationDefinitionNode Definition { get; }

    public ISelectionVariants RootSelectionVariants { get; }

    public ObjectType RootType { get; }

    public OperationType Type { get; }

    public IEnumerable<ISelectionVariants> SelectionVariants =>
        _selectionSets.Values;

    public ISelectionSet GetRootSelectionSet() =>
        RootSelectionVariants.GetSelectionSet(RootType);

    public ISelectionSet GetSelectionSet(
        SelectionSetNode selectionSet,
        IObjectType typeContext)
    {
        return _selectionSets.TryGetValue(selectionSet, out SelectionVariants? variants)
            ? variants.GetSelectionSet(typeContext)
            : SelectionSet.Empty;
    }

    public IEnumerable<IObjectType> GetPossibleTypes(SelectionSetNode selectionSet)
    {
        return _selectionSets.TryGetValue(selectionSet, out SelectionVariants? variants)
            ? variants.GetPossibleTypes()
            : Enumerable.Empty<IObjectType>();
    }

    public string Print()
    {
        OperationDefinitionNode operation =
            Definition.WithSelectionSet(Visit(RootSelectionVariants));
        var document = new DocumentNode(new[] { operation });
        return document.ToString();
    }

    public override string ToString() => Print();

    private SelectionSetNode Visit(ISelectionVariants selectionVariants)
    {
        var fragments = new List<InlineFragmentNode>();

        foreach (IObjectType objectType in selectionVariants.GetPossibleTypes())
        {
            var typeContext = (ObjectType)objectType;
            var selections = new List<ISelectionNode>();

            foreach (Selection selection in selectionVariants.GetSelectionSet(typeContext)
                .Selections.OfType<Selection>())
            {
                SelectionSetNode? selectionSetNode =
                    selection.SelectionSet is not null
                        ? Visit(_selectionSets[selection.SelectionSet])
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

    private FieldNode CreateSelection(Selection selection, SelectionSetNode? selectionSet)
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
