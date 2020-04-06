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
                            context.ArgumentNotUnique(argument, field);
                        }

                        if (arg.Type.IsNonNullType() &&
                            arg.DefaultValue.IsNull() &&
                            argument.Value.IsNull())
                        {
                            context.ArgumentRequired(argument, argument.Name.Value, field);
                        }
                    }
                    else
                    {
                        context.ArgumentDoesNotExist(argument, field);
                    }
                }

                for (int i = 0; i < field.Arguments.Count; i++)
                {
                    IInputField argument = field.Arguments[i];

                    if (argument.Type.IsNonNullType() &&
                        argument.DefaultValue.IsNull() &&
                        context.Names.Add(argument.Name))
                    {
                        context.ArgumentRequired(node, argument.Name, field);
                    }
                }
            }

            return Continue;
        }
    }
}
