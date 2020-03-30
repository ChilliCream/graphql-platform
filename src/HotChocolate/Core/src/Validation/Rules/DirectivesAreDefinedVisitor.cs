
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Validation
{
    /// <summary>
    /// GraphQL servers define what directives they support.
    /// For each usage of a directive, the directive must be available
    /// on that server.
    ///
    /// http://spec.graphql.org/June2018/#sec-Directives-Are-Defined
    /// </summary>
    internal sealed class DirectivesAreDefinedVisitor
        : DocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            DirectiveNode node,
            IDocumentValidatorContext context)
        {
            if (!context.Schema.TryGetDirectiveType(node.Name.Value, out _))
            {
                context.Errors.Add(
                    ErrorBuilder.New()
                        .SetMessage(
                            $"The specified directive `{node.Name.Value}` " +
                            "is not supported by the current schema.")
                        .AddLocation(node)
                        .SetPath(context.CreateErrorPath())
                        .SpecifiedBy("sec-Directives-Are-Defined")
                        .Build());
            }
            return Skip;
        }
    }
}
