using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Validation
{
    /// <summary>
    /// Each named operation definition must be unique within a document
    /// when referred to by its name.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Operation-Name-Uniqueness
    /// </summary>
    public class OperationNameUniquenessRule
        : IQueryValidationRule
    {
        public QueryValidationResult Validate(Schema schema, DocumentNode queryDocument)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }

            Dictionary<string, List<ISyntaxNode>> operations =
                CollectOperations(queryDocument);
            List<IQueryError> errors =
                CheckForRuleViolations(operations);

            if (errors.Count == 0)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(errors);
        }

        private Dictionary<string, List<ISyntaxNode>> CollectOperations(
            DocumentNode queryDocument)
        {
            Dictionary<string, List<ISyntaxNode>> operations =
                new Dictionary<string, List<ISyntaxNode>>();

            foreach (OperationDefinitionNode operation in queryDocument
                .Definitions.OfType<OperationDefinitionNode>()
                .Where(t => !string.IsNullOrEmpty(t.Name?.Value)))
            {
                if (!operations.TryGetValue(operation.Name.Value,
                    out List<ISyntaxNode> nodes))
                {
                    nodes = new List<ISyntaxNode>();
                    operations[operation.Name.Value] = nodes;
                }
                nodes.Add(operation);
            }

            return operations;
        }

        private List<IQueryError> CheckForRuleViolations(
            Dictionary<string, List<ISyntaxNode>> operations)
        {
            List<IQueryError> errors = new List<IQueryError>();
            foreach (KeyValuePair<string, List<ISyntaxNode>> operation in operations)
            {
                if (operation.Value.Count > 1)
                {
                    errors.Add(new ValidationError(
                        $"The operation name `{operation.Key}` is not unique.",
                        operation.Value));
                }
            }
            return errors;
        }
    }

    public class EmptySelectionSetRule
       : IQueryValidationRule
    {
        public QueryValidationResult Validate(Schema schema, DocumentNode queryDocument)
        {
            if (schema == null)
            {
                throw new ArgumentNullException(nameof(schema));
            }

            if (queryDocument == null)
            {
                throw new ArgumentNullException(nameof(queryDocument));
            }


        }


    }

    internal class QueryVisitor
    {
        private readonly Schema _schema;

        protected virtual void VisitDocument(DocumentNode node)
        {
            foreach (OperationDefinitionNode operation in node.Definitions
                .OfType<OperationDefinitionNode>())
            {
                VisitOperationDefinition(operation,
                    ImmutableStack<ISyntaxNode>.Empty);
            }
        }

        protected virtual void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            IType operationType = _schema.GetOperationType(operation.Operation);
            VisitSelectionSet(operation.SelectionSet, operationType, path);
        }

        protected virtual void VisitVariableDefinition(VariableDefinitionNode node) { }
        protected virtual void VisitVariable(VariableNode node) { }

        protected virtual void VisitSelectionSet(
            SelectionSetNode selectionSet,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (ISelectionNode selection in selectionSet.Selections)
            {
                if (selection is FieldNode field)
                {
                    VisitField(field, type);
                }

                if (selection is FragmentSpreadNode fragmentSpread)
                {
                    VisitFragmentSpread(fragmentSpread, type);
                }

                if (selection is InlineFragmentNode inlineFragment)
                {
                    VisitInlineFragment(inlineFragment, type);
                }
            }
        }

        protected virtual void VisitField(
            FieldNode field,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {
            ImmutableStack<ISyntaxNode> current = path.Push(field);

            foreach (ArgumentNode argument in field.Arguments)
            {
                VisitArgument(argument, type, current);
            }


            VisitSelectionSet(field.SelectionSet, );
        }

        protected Field GetField(IType type, FieldNode field)
        {
            if (type == _schema.QueryType)
            {

            }

            if (type is ObjectType objectType
                && objectType.Fields.TryGetValue(field.Name.Value, out Field f))
            {

            }

            if (type is InterfaceType interfaceType
                && interfaceType.Fields.TryGetValue(field.Name.Value, out f))
            {

            }

        }

        protected virtual void VisitArgument(
            ArgumentNode node,
            IType type,
            ImmutableStack<ISyntaxNode> path)
        {

        }

        protected virtual void VisitFragmentSpread(FragmentSpreadNode fragmentSpread, IType type) { }
        protected virtual void VisitInlineFragment(InlineFragmentNode node, IType type) { }
        protected virtual void VisitFragmentDefinition(FragmentDefinitionNode node) { }
    }
}
