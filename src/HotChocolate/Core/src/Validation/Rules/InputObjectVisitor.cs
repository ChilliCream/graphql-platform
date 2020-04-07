using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Validation.Rules
{
    /// <summary>
    /// Every input field provided in an input object value must be defined in
    /// the set of possible fields of that input object’s expected type.
    ///
    /// http://spec.graphql.org/June2018/#sec-Input-Object-Field-Names
    ///
    /// AND
    ///
    /// Input objects must not contain more than one field of the same name,
    /// otherwise an ambiguity would exist which includes an ignored portion
    /// of syntax.
    ///
    /// http://spec.graphql.org/June2018/#sec-Input-Object-Field-Uniqueness
    ///
    /// AND
    ///
    /// Input object fields may be required. Much like a field may have
    /// required arguments, an input object may have required fields.
    ///
    /// An input field is required if it has a non‐null type and does not have
    /// a default value. Otherwise, the input object field is optional.
    ///
    /// http://spec.graphql.org/June2018/#sec-Input-Object-Required-Fields
    /// </summary>
    internal sealed class InputObjectVisitor : TypeDocumentValidatorVisitor
    {
        public InputObjectVisitor()
            : base(new SyntaxVisitorOptions
            {
                VisitDirectives = true,
                VisitArguments = true
            })
        {
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            if (IntrospectionFields.TypeName.Equals(node.Name.Value))
            {
                return Skip;
            }
            else if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is IComplexOutputType ot &&
                ot.Fields.TryGetField(node.Name.Value, out IOutputField of))
            {
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
            if (context.Schema.TryGetDirectiveType(node.Name.Value, out DirectiveType d))
            {
                context.Directives.Push(d);
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

        protected override ISyntaxVisitorAction Enter(
            ArgumentNode node,
            IDocumentValidatorContext context)
        {
            if (context.Directives.TryPeek(out DirectiveType directive))
            {
                if (directive.Arguments.TryGetField(node.Name.Value, out Argument argument))
                {
                    context.InputFields.Push(argument);
                    context.Types.Push(argument.Type);
                    return Continue;
                }
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
            else if (context.OutputFields.TryPeek(out IOutputField field))
            {
                if (field.Arguments.TryGetField(node.Name.Value, out IInputField argument))
                {
                    context.InputFields.Push(argument);
                    context.Types.Push(argument.Type);
                    return Continue;
                }
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
            else
            {
                context.UnexpectedErrorsDetected = true;
                return Skip;
            }
        }

        protected override ISyntaxVisitorAction Leave(
            ArgumentNode node,
            IDocumentValidatorContext context)
        {
            context.InputFields.Pop();
            context.Types.Pop();
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            for (int i = 0; i < node.Fields.Count; i++)
            {
                ObjectFieldNode field = node.Fields[i];
                if (!context.Names.Add(field.Name.Value))
                {
                    context.Errors.Add(context.InputFieldAmbiguous(field));
                }
            }

            var type = (InputObjectType)context.Types.Peek().NamedType();
            if (context.Names.Count < type.Fields.Count)
            {
                for (int i = 0; i < type.Fields.Count; i++)
                {
                    IInputField field = type.Fields[i];
                    if (field.Type.IsNonNullType() &&
                        field.DefaultValue.IsNull() &&
                        context.Names.Add(field.Name))
                    {
                        InputFieldRequiredError(node, field.Name, context);
                    }
                }
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            IDocumentValidatorContext context)
        {
            if (context.Types.TryPeek(out IType type) &&
                type.NamedType() is InputObjectType it &&
                it.Fields.TryGetField(node.Name.Value, out InputField field))
            {
                if (field.Type.IsNonNullType() &&
                    field.DefaultValue.IsNull() &&
                    node.Value.IsNull())
                {
                    InputFieldRequiredError(node, field.Name, context);
                }

                context.InputFields.Push(field);
                context.Types.Push(field.Type);
                return Continue;
            }
            else
            {
                context.Errors.Add(context.InputFieldDoesNotExist(node));
                return Skip;
            }
        }

        protected override ISyntaxVisitorAction Leave(
            ObjectFieldNode node,
            IDocumentValidatorContext context)
        {
            context.InputFields.Pop();
            context.Types.Pop();
            return Continue;
        }

        private static void InputFieldRequiredError(
            ISyntaxNode node,
            string fieldName,
            IDocumentValidatorContext context)
        {
            context.Errors.Add(
                ErrorBuilder.New()
                    .SetMessage("`{0}` is a required field and cannot be null.", fieldName)
                    .AddLocation(node)
                    .SetPath(context.CreateErrorPath())
                    .SetExtension("field", fieldName)
                    .SpecifiedBy("sec-Input-Object-Required-Fields")
                    .Build());
        }
    }
}
