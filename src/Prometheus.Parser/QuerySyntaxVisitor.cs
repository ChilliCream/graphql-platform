using System;
using System.Collections.Generic;
using GraphQLParser.AST;
using Prometheus.Abstractions;

namespace Prometheus.Parser
{
    public class QuerySyntaxVisitor
        : SyntaxNodeWalker
    {
        private readonly List<IQueryDefinition> _definitions = new List<IQueryDefinition>();
        private readonly Stack<List<ISelection>> _selectionStack = new Stack<List<ISelection>>();

        public IReadOnlyCollection<IQueryDefinition> Definitions => _definitions;

        protected override void VisitOperationDefinition(GraphQLOperationDefinition node)
        {
            _selectionStack.Push(new List<ISelection>());

            Visit(node.SelectionSet);

            List<ISelection> selections = _selectionStack.Pop();
            _definitions.Add(new OperationDefinition(
               node.Name?.Value ?? string.Empty,
               node.Operation.Map(),
               node.VariableDefinitions.Map(),
               selections));
        }

        protected override void VisitField(GraphQLFieldSelection node)
        {
            _selectionStack.Push(new List<ISelection>());

            Visit(node.SelectionSet);

            List<ISelection> selections = _selectionStack.Pop();
            _selectionStack.Peek().Add(new Field(
                node.Alias?.Value,
                node.Name.Value,
                node.Arguments.Map(),
                node.Directives.Map(),
                selections));
        }

        protected override void VisitInlineFragment(GraphQLInlineFragment node)
        {
            _selectionStack.Push(new List<ISelection>());

            Visit(node.SelectionSet);

            List<ISelection> selections = _selectionStack.Pop();
            _selectionStack.Peek().Add(new InlineFragment(
                (NamedType)node.TypeCondition.Map(),
                selections,
                node.Directives.Map()));
        }

        protected override void VisitFragmentSpread(GraphQLFragmentSpread node)
        {
            _selectionStack.Peek().Add(new FragmentSpread(
                node.Name.Value,
                node.Directives.Map()));
        }

        protected override void VisitFragmentDefinition(GraphQLFragmentDefinition node)
        {
            _selectionStack.Push(new List<ISelection>());

            Visit(node.SelectionSet);

            List<ISelection> selections = _selectionStack.Pop();
            _definitions.Add(new FragmentDefinition(
                node.Name.Value,
                (NamedType)node.TypeCondition.Map(),
                selections,
                node.Directives.Map()));
        }
    }
}