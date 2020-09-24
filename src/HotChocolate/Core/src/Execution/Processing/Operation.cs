using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    internal sealed class Operation : IPreparedOperation
    {
        private readonly IReadOnlyDictionary<SelectionSetNode, SelectionVariants> _selectionSets;

        public Operation(
            string id,
            DocumentNode document,
            OperationDefinitionNode definition,
            ObjectType rootType,
            IReadOnlyDictionary<SelectionSetNode, SelectionVariants> selectionSets)
        {
            Id = id;
            Name = definition.Name?.Value;
            Document = document;
            Definition = definition;
            RootSelectionVariants = selectionSets[definition.SelectionSet];
            RootType = rootType;
            Type = definition.Operation;
            _selectionSets = selectionSets;
            ProposedTaskCount = 1;
        }

        public string Id { get; }

        public NameString? Name { get; }

        public DocumentNode Document { get; }

        public OperationDefinitionNode Definition { get; }

        public ISelectionVariants RootSelectionVariants { get; }

        public ObjectType RootType { get; }

        public OperationType Type { get; }

        public int ProposedTaskCount { get; }

        public ISelectionSet GetRootSelectionSet() =>
            RootSelectionVariants.GetSelectionSet(RootType);

        public ISelectionSet GetSelectionSet(
            SelectionSetNode selectionSet,
            ObjectType typeContext)
        {
            return _selectionSets.TryGetValue(selectionSet, out SelectionVariants? variants)
                ? variants.GetSelectionSet(typeContext)
                : SelectionSet.Empty;
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
                var typeContext = (ObjectType) objectType;
                var selections = new List<ISelectionNode>();

                foreach (Selection selection in selectionVariants.GetSelectionSet(typeContext)
                    .Selections.OfType<Selection>())
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
                            selection.SyntaxNode.Name,
                            selection.SyntaxNode.Alias,
                            directives,
                            selection.SyntaxNode.Arguments,
                            null));
                    }
                    else
                    {
                        selections.Add(new FieldNode(
                            null,
                            selection.SyntaxNode.Name,
                            selection.SyntaxNode.Alias,
                            directives,
                            selection.SyntaxNode.Arguments,
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
