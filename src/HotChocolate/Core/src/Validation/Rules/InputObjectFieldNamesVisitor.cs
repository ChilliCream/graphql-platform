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
    /// </summary>
    internal sealed class InputObjectFieldNamesVisitor : TypeDocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
           ObjectValueNode node,
           IDocumentValidatorContext context)
        {
            foreach (ObjectFieldNode fieldValue in objectValue.Fields)
            {
                if (type.Fields.TryGetField(fieldValue.Name.Value,
                    out InputField inputField))
                {
                    if (inputField.Type is InputObjectType inputFieldType
                        && fieldValue.Value is ObjectValueNode ov)
                    {
                        VisitObjectValue(inputFieldType, ov);
                    }
                }
                else
                {
                    Errors.Add(new ValidationError(
                        "The specified input object field " +
                        $"`{fieldValue.Name.Value}` does not exist.",
                        fieldValue));
                }
            }
        }
    }
}
