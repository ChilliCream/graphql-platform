using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation
{
    /// <summary>
    /// If any operation defines more than one variable with the same name,
    /// it is ambiguous and invalid. It is invalid even if the type of the
    /// duplicate variable is the same.
    ///
    /// http://spec.graphql.org/June2018/#sec-Validation.Variables
    /// </summary>
    internal sealed class VariableUniquenessVisitor
        : DocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            context.DeclaredVariables.Clear();
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidatorContext context)
        {
            string name = node.Variable.Name.Value;
            if (!context.DeclaredVariables.Contains(name))
            {
                context.DeclaredVariables.Add(node.Variable.Name.Value);
            }
            else
            {
                // TODO : Resources
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                           "A document containing operations that " +
                           "define more than one variable with the same " +
                           "name is invalid for execution.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SpecifiedBy("sec-Validation.Variables")
                        .Build());
            }
            return Skip;
        }
    }
}
