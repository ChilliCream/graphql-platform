using System;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// GraphQL execution will only consider the executable definitions
    /// Operation and Fragment.
    ///
    /// Type system definitions and extensions are not executable,
    /// and are not considered during execution.
    ///
    /// To avoid ambiguity, a document containing TypeSystemDefinition
    /// is invalid for execution.
    ///
    /// GraphQL documents not intended to be directly executed may
    /// include TypeSystemDefinition.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Executable-Definitions
    /// </summary>
    internal sealed class ExecutableDefinitionsRule
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

            IDefinitionNode typeSystemNode = null;

            for (int i = 0; i < queryDocument.Definitions.Count; i++)
            {
                IDefinitionNode node = queryDocument.Definitions[i];
                if (node is ITypeSystemDefinitionNode
                    || node is ITypeSystemExtensionNode)
                {
                    typeSystemNode = node;
                    break;
                }
            }

            if (typeSystemNode is null)
            {
                return QueryValidationResult.OK;
            }

            return new QueryValidationResult(
                new ValidationError(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution.", typeSystemNode));
        }
    }
}
