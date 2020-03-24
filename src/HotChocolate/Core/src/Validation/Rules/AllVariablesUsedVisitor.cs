using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// All variables defined by an operation must be used in that operation
    /// or a fragment transitively included by that operation.
    ///
    /// Unused variables cause a validation error.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-All-Variables-Used
    ///
    /// AND
    ///
    /// Variables are scoped on a per‐operation basis. That means that
    /// any variable used within the context of an operation must be defined
    /// at the top level of that operation
    ///
    /// https://facebook.github.io/graphql/June2018/#sec-All-Variable-Uses-Defined
    /// </summary>
    internal sealed class AllVariablesUsedVisitor : DocumentValidationVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidationContext context)
        {
            context.UnusedVariables.Add(node.Variable.Name.Value);
            context.DeclaredVariables.Add(node.Variable.Name.Value);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableNode node,
            IDocumentValidationContext context)
        {
            context.UsedVariables.Remove(node.Name.Value);
            return Continue;
        }

        protected override ISyntaxVisitorAction Leave(
            OperationDefinitionNode node,
            IDocumentValidationContext context)
        {
            context.UnusedVariables.ExceptWith(context.UsedVariables);
            context.UsedVariables.ExceptWith(context.DeclaredVariables);

            if (context.UnusedVariables.Count > 0)
            {
                // TODO : Resources
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The following variables were not used: " +
                            $"{string.Join(", ", context.UnusedVariables)}.")
                        .AddLocation(node)
                        .Build());
            }

            if (context.UsedVariables.Count > 0)
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The following variables were not declared: " +
                            $"{string.Join(", ", context.UsedVariables)}.")
                        .AddLocation(node)
                        .Build());
            }

            return Continue;
        }
    }
}
