using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// Every argument provided to a field or directive must be defined
    /// in the set of possible arguments of that field or directive.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Argument-Names
    ///
    /// AND
    ///
    /// Fields and directives treat arguments as a mapping of argument name
    /// to value.
    ///
    /// More than one argument with the same name in an argument set
    /// is ambiguous and invalid.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Argument-Uniqueness
    ///
    /// AND
    ///
    /// Arguments can be required. An argument is required if the argument
    /// type is non‐null and does not have a default value. Otherwise,
    /// the argument is optional.
    ///
    /// http://facebook.github.io/graphql/June2018/#sec-Required-Arguments
    /// </summary>
    internal sealed class ArgumentsVisitor : TypeDocumentValidatorVisitor
    {
        public ArgumentsVisitor()
            : base(new SyntaxVisitorOptions
                {
                    VisitDirectives = true
                })
        {
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            if (!context.IsInError.PeekOrDefault(true) &&
                context.OutputFields.TryPeek(out IOutputField field))
            {
                ValidateArguments(
                    context, node, node.Arguments,
                    field.Arguments, field: field);
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            DirectiveNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            if (!context.IsInError.PeekOrDefault(true) &&
                context.Directives.TryPeek(out DirectiveType directiveType))
            {
                ValidateArguments(
                    context, node, node.Arguments,
                    directiveType.Arguments, directive: directiveType);
            }

            return Continue;
        }

        private void ValidateArguments(
            IDocumentValidatorContext context,
            ISyntaxNode node,
            IReadOnlyList<ArgumentNode> argumentNodes,
            IFieldCollection<IInputField> arguments,
            IOutputField? field = null,
            DirectiveType? directive = null)
        {
            context.Names.Clear();

            for (int i = 0; i < argumentNodes.Count; i++)
            {
                ArgumentNode argument = argumentNodes[i];

                if (arguments.TryGetField(argument.Name.Value, out IInputField arg))
                {
                    if (!context.Names.Add(argument.Name.Value))
                    {
                        context.Errors.Add(context.ArgumentNotUnique(
                            argument, field, directive));
                    }

                    if (arg.Type.IsNonNullType() &&
                        arg.DefaultValue.IsNull() &&
                        argument.Value.IsNull())
                    {
                        context.Errors.Add(context.ArgumentRequired(
                            argument, argument.Name.Value, field, directive));
                    }
                }
                else
                {
                    context.Errors.Add(context.ArgumentDoesNotExist(
                        argument, field, directive));
                }
            }

            for (int i = 0; i < arguments.Count; i++)
            {
                IInputField argument = arguments[i];

                if (argument.Type.IsNonNullType() &&
                    argument.DefaultValue.IsNull() &&
                    context.Names.Add(argument.Name))
                {
                    context.Errors.Add(context.ArgumentRequired(
                        node, argument.Name, field, directive));
                }
            }
        }
    }
}
