using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// Each named operation definition must be unique within a document
    /// when referred to by its name.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Operation-Name-Uniqueness
    /// </summary>
    internal sealed class OperationNameUniquenessRule
        : IQueryValidationRule
    {
        public QueryValidationResult Validate(
            ISchema schema,
            DocumentNode queryDocument)
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
            List<IError> errors =
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

        private List<IError> CheckForRuleViolations(
            Dictionary<string, List<ISyntaxNode>> operations)
        {
            List<IError> errors = new List<IError>();
            foreach (KeyValuePair<string, List<ISyntaxNode>> operation in
                operations)
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
}
