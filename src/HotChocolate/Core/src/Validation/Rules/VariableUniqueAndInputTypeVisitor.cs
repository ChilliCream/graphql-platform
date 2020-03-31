using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    /// <summary>
    /// If any operation defines more than one variable with the same name,
    /// it is ambiguous and invalid. It is invalid even if the type of the
    /// duplicate variable is the same.
    ///
    /// http://spec.graphql.org/June2018/#sec-Validation.Variables
    /// 
    /// AND
    /// 
    /// Variables can only be input types. Objects,
    /// unions, and interfaces cannot be used as inputs.
    ///
    /// http://spec.graphql.org/June2018/#sec-Variables-Are-Input-Types
    /// </summary>
    internal sealed class VariableUniqueAndInputTypeVisitor
        : TypeDocumentValidatorVisitor
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
            string variableName = node.Variable.Name.Value;

            if (context.Schema.TryGetType(
                    node.Type.NamedType().Name.Value, out INamedType type) &&
                !type.IsInputType())
            {
                context.Errors.Add(
                   ErrorBuilder.New()
                       .SetMessage(
                            "The type of variable " +
                            $"`{variableName}` " +
                            "is not an input type.")
                       .AddLocation(node)
                       .SetPath(context.CreateErrorPath())
                       .SetExtension("variable", variableName)
                       .SetExtension("variableType", node.Type.ToString())
                       .SetExtension("locationType", context.Types.Peek().Visualize())
                       .SpecifiedBy("sec-Variables-Are-Input-Types")
                       .Build());
            }

            if (!context.DeclaredVariables.Contains(variableName))
            {
                context.DeclaredVariables.Add(variableName);
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
                        .SetExtension("variable", variableName)
                        .SetExtension("variableType", node.Type.ToString())
                        .SetExtension("locationType", context.Types.Peek().Visualize())
                        .SpecifiedBy("sec-Validation.Variables")
                        .Build());
            }
            return Skip;
        }
    }
}
