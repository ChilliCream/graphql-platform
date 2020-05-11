using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using PSS = HotChocolate.Execution.Utilities.PreparedSelectionSet;

namespace HotChocolate.Execution.Utilities
{
    internal sealed class PreparedOperation : IPreparedOperation
    {
        private static IPreparedSelectionList _empty = 
            new PreparedSelectionList(new IPreparedSelection[0], true);
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
            SelectionSet = selectionSets[definition.SelectionSet];
            RootType = rootType;
            Type = definition.Operation;
            _selectionSets = selectionSets;
        }

        public string Id { get; }

        public NameString? Name { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Definition { get; }

        public IPreparedSelectionSet SelectionSet { get; }

        public ObjectType RootType { get; }

        public OperationType Type { get; }

        public IPreparedSelectionList GetSelections(
            SelectionSetNode selectionSet,
            ObjectType typeContext)
        {
            if (_selectionSets.TryGetValue(selectionSet, out PSS? preparedSelectionSet))
            {
                return preparedSelectionSet.GetSelections(typeContext);
            }
            return _empty;
        }

        public string Print()
        {
            var operation = Definition.WithSelectionSet(Visit(SelectionSet));
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

                foreach (IPreparedSelection selection in selectionSet.GetSelections(typeContext))
                {
                    if (selection.SelectionSet is null)
                    {
                        selections.Add(new FieldNode(
                            null,
                            selection.Selection.Name,
                            selection.Selection.Alias,
                            Array.Empty<DirectiveNode>(),
                            selection.Selection.Arguments,
                            null));
                    }
                    else
                    {
                        selections.Add(new FieldNode(
                            null,
                            selection.Selection.Name,
                            selection.Selection.Alias,
                            Array.Empty<DirectiveNode>(),
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
