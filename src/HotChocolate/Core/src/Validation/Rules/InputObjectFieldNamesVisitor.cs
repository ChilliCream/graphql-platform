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
           ObjectFieldNode node,
           IDocumentValidatorContext context)
        {
            if (!context.IsInError.PeekOrDefault(true))
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            "The specified input object field " +
                            $"`{node.Name.Value}` does not exist.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SetExtension("field", node.Name.Value)
                        .SpecifiedBy("sec-Executable-Definitions")
                        .Build());
                return Skip;
            }
            return Continue;
        }
    }
}
