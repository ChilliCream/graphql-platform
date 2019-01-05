using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Validation
{
    /// <summary>
    /// If any operation defines more than one variable with the same name,
    /// it is ambiguous and invalid. It is invalid even if the type of the
    /// duplicate variable is the same.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Validation.Variables
    /// </summary>
    internal sealed class VariableUniquenessRule
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

            HashSet<string> names = new HashSet<string>();

            foreach (OperationDefinitionNode operation in queryDocument
                .Definitions.OfType<OperationDefinitionNode>())
            {
                foreach (VariableDefinitionNode variableDefinition in
                    operation.VariableDefinitions)
                {
                    if (names.Contains(variableDefinition.Variable.Name.Value))
                    {
                        return new QueryValidationResult(
                            new ValidationError(
                                "A document containing operations that " +
                                "define more than one variable with the same " +
                                "name is invalid for execution.", operation));
                    }
                    names.Add(variableDefinition.Variable.Name.Value);
                }
                names.Clear();
            }

            return QueryValidationResult.OK;
        }
    }
}
