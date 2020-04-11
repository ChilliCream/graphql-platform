using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

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
    internal sealed class ArgumentVisitor : TypeDocumentValidatorVisitor
    {
        public ArgumentVisitor()
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

            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                ValidateArguments(
                    context, node, node.Arguments,
                    TypeNameField.Arguments, field: TypeNameField);

                return Skip;
            }
            else if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ot &&
                ot.Fields.TryGetField(node.Name.Value, out IOutputField of))
            {
                ValidateArguments(
                    context, node, node.Arguments,
                    of.Arguments, field: of);

                context.OutputFields.Push(of);
                context.Types.Push(of.Type);
                return Continue;
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
        }

        protected override ISyntaxVisitorAction Leave(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            context.Types.Pop();
            context.OutputFields.Pop();
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            DirectiveNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            if (context.Schema.TryGetDirectiveType(node.Name.Value, out DirectiveType d))
            {
                context.Directives.Push(d);

                ValidateArguments(
                    context, node, node.Arguments,
                    d.Arguments, directive: d);

                return Continue;
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
        }

        protected override ISyntaxVisitorAction Leave(
            DirectiveNode node,
            IDocumentValidatorContext context)
        {
            context.Directives.Pop();
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
