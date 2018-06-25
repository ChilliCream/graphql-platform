using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Execution
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
    /// http://facebook.github.io/graphql/draft/#sec-Executable-Definitions
    /// </summary>
    public class ExecutableDefinitionsValidator
        : IQueryValidator
    {
        public QueryValidationResult Validate(Schema schema, DocumentNode queryDocument)
        {
            ITypeSystemDefinitionNode typeSystemNode = queryDocument.Definitions
                .OfType<ITypeSystemDefinitionNode>().FirstOrDefault();
            if (typeSystemNode == null)
            {
                return QueryValidationResult.OK;
            }
            return new QueryValidationResult(
                new QueryError(
                    "A document containing TypeSystemDefinition " +
                    "is invalid for execution."));
        }
    }
}
