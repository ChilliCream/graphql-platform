using System.Collections.Generic;
using System.Collections.Immutable;
using HotChocolate.Internal;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class VariablesAreInputTypesVisitor
        : QueryVisitorErrorBase
    {
        public VariablesAreInputTypesVisitor(ISchema schema)
            : base(schema)
        {
        }

        protected override void VisitOperationDefinition(
            OperationDefinitionNode operation,
            ImmutableStack<ISyntaxNode> path)
        {
            foreach (VariableDefinitionNode variableDefinition in
                operation.VariableDefinitions)
            {
                if (Schema.TryGetTypeFromAst(variableDefinition.Type,
                    out IType type))
                {
                    if (!type.IsInputType())
                    {
                        Errors.Add(new ValidationError(
                            "The type of variable " +
                            $"`{variableDefinition.Variable.Name.Value}` " +
                            "is not an input type.", variableDefinition));
                    }
                }
            }
        }
    }
}
