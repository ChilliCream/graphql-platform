using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;
using PSS = HotChocolate.Execution.Utilities.PreparedSelectionSet;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedOperation : IPreparedOperation
    {
        private readonly IReadOnlyDictionary<SelectionSetNode, PSS> _selectionSets;

        public PreparedOperation(
            string id,
            DocumentNode document,
            OperationDefinitionNode definition,
            ObjectType rootType,
            IReadOnlyDictionary<SelectionSetNode, PSS> selectionSets)
        {
            Id = id;
            Name = definition.Name?.Value;
            Document = document;
            Definition = definition;
            RootSelectionSet = selectionSets[definition.SelectionSet];
            RootType = rootType;
            Type = definition.Operation;
            _selectionSets = selectionSets;
            ProposedTaskCount = 1;
        }

        public string Id { get; }

        public NameString? Name { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Definition { get; }

        public IPreparedSelectionSet RootSelectionSet { get; }

        public ObjectType RootType { get; }

        public OperationType Type { get; }

        public int ProposedTaskCount { get; }

        public IPreparedSelectionList GetRootSelections() =>
            RootSelectionSet.GetSelections(RootType);

        public IPreparedSelectionList GetSelections(
            SelectionSetNode selectionSet,
            ObjectType typeContext)
        {
            return _selectionSets.TryGetValue(selectionSet, out PSS? preparedSelectionSet)
                ? preparedSelectionSet.GetSelections(typeContext)
                : PreparedSelectionList.Empty;
        }

        public string Print()
        {
            var operation = Definition.WithSelectionSet(Visit(RootSelectionSet));
            var document = new DocumentNode(new[] { operation });
            return document.ToString();
        }

        public override string ToString() => Print();

        private SelectionSetNode Visit(IPreparedSelectionSet selectionSet)
        {
            var fragments = new List<InlineFragmentNode>();

            foreach (ObjectType typeContext in selectionSet.GetPossibleTypes())
            {
                var selections = new List<ISelectionNode>();

                foreach (PreparedSelection selection in selectionSet.GetSelections(typeContext)
                    .OfType<PreparedSelection>())
                {
                    var directives = new List<DirectiveNode>();

                    if (selection.IncludeConditions is { })
                    {
                        foreach (SelectionIncludeCondition visibility in selection.IncludeConditions)
                        {
                            if (visibility.Skip is { })
                            {
                                directives.Add(
                                    new DirectiveNode(
                                        "skip",
                                        new ArgumentNode("if", visibility.Skip)));
                            }

                            if (visibility.Include is { })
                            {
                                directives.Add(
                                    new DirectiveNode(
                                        "include",
                                        new ArgumentNode("if", visibility.Include)));
                            }
                        }
                    }

                    if (selection.IsInternal)
                    {
                        directives.Add(new DirectiveNode("_internal"));
                    }

                    if (selection.SelectionSet is null)
                    {
                        selections.Add(new FieldNode(
                            null,
                            selection.Selection.Name,
                            selection.Selection.Alias,
                            directives,
                            selection.Selection.Arguments,
                            null));
                    }
                    else
                    {
                        selections.Add(new FieldNode(
                            null,
                            selection.Selection.Name,
                            selection.Selection.Alias,
                            directives,
                            selection.Selection.Arguments,
                            Visit(_selectionSets[selection.SelectionSet])));
                    }
                }

                fragments.Add(new InlineFragmentNode(
                    null,
                    new NamedTypeNode(typeContext.Name),
                    Array.Empty<DirectiveNode>(),
                    new SelectionSetNode(selections)));
            }

            return new SelectionSetNode(fragments);
        }
    }
}
