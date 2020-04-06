using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
{
    internal sealed class ArgumentsVisitor : DocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            if (!context.IsInError.PeekOrDefault(true) &&
                context.OutputFields.TryPeek(out IOutputField field))
            {
                for (int i = 0; i < node.Arguments.Count; i++)
                {
                    ArgumentNode argument = node.Arguments[i];

                    if (field.Arguments.TryGetField(argument.Name.Value, out IInputField arg))
                    {
                        if (!context.Names.Add(argument.Name.Value))
                        {
                            // error 1
                        }

                        if (arg.Type.IsNonNullType() &&
                            arg.DefaultValue.IsNull() &&
                            argument.Value.IsNull())
                        {
                            // error 3
                        }
                    }
                    else
                    {
                        // error 2
                    }
                }

                for (int i = 0; i < field.Arguments.Count; i++)
                {
                    IInputField argument = field.Arguments[i];

                    if (argument.Type.IsNonNullType() &&
                        argument.DefaultValue.IsNull() &&
                        context.Names.Add(argument.Name))
                    {
                        // error 3
                    }
                }
            }

            return Continue;
        }
    }

    internal static class ErrorHelper
    {
        public static IError VariableNotUsed(
            this IDocumentValidatorContext context,
            OperationDefinitionNode node)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "The following variables were not used: " +
                    $"{string.Join(", ", context.Unused)}.")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-All-Variables-Used")
                .Build();
        }

        public static IError VariableNotDeclared(
            this IDocumentValidatorContext context,
            OperationDefinitionNode node)
        {
            return ErrorBuilder.New()
                .SetMessage(
                    "The following variables were not declared: " +
                    $"{string.Join(", ", context.Used)}.")
                .AddLocation(node)
                .SetPath(context.CreateErrorPath())
                .SpecifiedBy("sec-All-Variable-Uses-Defined")
                .Build();
        }

        public static IError VariableIsNotCompatible(
            this IDocumentValidatorContext context,
            VariableNode variable,
            VariableDefinitionNode variableDefinition)
        {
            string variableName = variableDefinition.Variable.Name.Value;

            return ErrorBuilder.New()
                .SetMessage(
                    $"The variable `{variableName}` is not compatible " +
                    "with the type of the current location.")
                .AddLocation(variable)
                .SetPath(context.CreateErrorPath())
                .SetExtension("variable", variableName)
                .SetExtension("variableType", variableDefinition.Type.ToString())
                .SetExtension("locationType", context.Types.Peek().Visualize())
                .SpecifiedBy("sec-All-Variable-Usages-are-Allowed")
                .Build();
        }



        public static IError ArgumentNotUnique()
        {

        }
    }
}
