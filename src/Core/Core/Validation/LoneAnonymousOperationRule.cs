using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// GraphQL allows a short‐hand form for defining query operations
    /// when only that one operation exists in the document.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Lone-Anonymous-Operation
    /// </summary>
    internal sealed class LoneAnonymousOperationRule
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

            OperationDefinitionNode[] operations =
                GetAllOperations(queryDocument);
            if (HasAnonymousOperation(operations) && operations.Length > 1)
            {
                return new QueryValidationResult(new ValidationError(
                    "GraphQL allows a short‐hand form for defining query " +
                    "operations when only that one operation exists in the " +
                    "document.", operations));
            }
            return QueryValidationResult.OK;
        }

        private bool HasAnonymousOperation(OperationDefinitionNode[] operations)
        {
            return operations.Any(t => string.IsNullOrEmpty(t.Name?.Value));
        }

        private OperationDefinitionNode[] GetAllOperations(
            DocumentNode queryDocument)
        {
            return queryDocument.Definitions
                .OfType<OperationDefinitionNode>()
                .ToArray();
        }
    }
}
