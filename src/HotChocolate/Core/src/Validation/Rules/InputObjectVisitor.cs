using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Validation
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
        protected override ISyntaxVisitorAction Enter(
            ObjectValueNode node,
            IDocumentValidatorContext context)
        {
            context.Names.Clear();

            for (int i = 0; i < node.Fields.Count; i++)
            {
                string name = node.Fields[i].Name.Value;
                if (!context.Names.Add(name))
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage("Field `{0}` is ambiguous.", name)
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SetExtension("field", name)
                            .SpecifiedBy("sec-Input-Object-Field-Uniqueness")
                            .Build());
                }
            }

            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            ObjectFieldNode node,
            IDocumentValidatorContext context)
        {
            if (context.IsInError.PeekOrDefault(true))
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The specified input object field " +
                            $"`{node.Name.Value}` does not exist.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("field", node.Name.Value)
                        .SpecifiedBy("sec-Input-Object-Field-Names")
                        .Build());
                return Skip;
            }
            else
            {
                IInputField field = context.InputFields.Peek();
                if (field.Type.IsNonNullType() &&
                    field.DefaultValue.IsNull() &&
                    node.Value.IsNull())
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage("`{0}` is a required field and cannot be null.", field.Name)
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SetExtension("field", node.Name.Value)
                            .SpecifiedBy("sec-Input-Object-Required-Fields")
                            .Build());
                }
            }
            return Continue;
        }
    }
}
