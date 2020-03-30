using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using DirectiveLoc = HotChocolate.Types.DirectiveLocation;

namespace HotChocolate.Validation
{
    /// <summary>
    /// GraphQL servers define what directives they support.
    /// For each usage of a directive, the directive must be available
    /// on that server.
    ///
    /// http://spec.graphql.org/June2018/#sec-Directives-Are-Defined
    ///
    /// AND
    ///
    /// GraphQL servers define what directives they support and where they
    /// support them.
    ///
    /// For each usage of a directive, the directive must be used in a
    /// location that the server has declared support for.
    ///
    /// http://spec.graphql.org/June2018/#sec-Directives-Are-In-Valid-Locations
    /// </summary>
    internal sealed class DirectivesAreDefinedVisitor
        : DocumentValidatorVisitor
    {
        protected override ISyntaxVisitorAction Enter(
            ISyntaxNode node,
            IDocumentValidatorContext context)
        {
            switch (node.Kind)
            {
                case NodeKind.Field:
                case NodeKind.SelectionSet:
                case NodeKind.FragmentDefinition:
                case NodeKind.FragmentSpread:
                case NodeKind.InlineFragment:
                case NodeKind.Directive:
                case NodeKind.OperationDefinition:
                case NodeKind.Document:
                    return base.Enter(node, context);

                default:
                    return Skip;
            }
        }

        protected override ISyntaxVisitorAction Enter(
            DirectiveNode node,
            IDocumentValidatorContext context)
        {
            if (context.Schema.TryGetDirectiveType(node.Name.Value, out DirectiveType? dt))
            {
                if (context.Path.TryPeek(out ISyntaxNode parent) &&
                    TryLookupLocation(parent, out DirectiveLoc location) &&
                    !dt.Locations.Contains(location))
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage(
                                "The specified directive is not valid the current location.")
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SpecifiedBy("sec-Directives-Are-In-Valid-Locations")
                            .Build());
                }
            }
            else
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

        private static bool TryLookupLocation(ISyntaxNode node, out DirectiveLoc location)
        {
            switch (node.Kind)
            {
                case NodeKind.Field:
                    location = DirectiveLoc.Field;
                    return true;

                case NodeKind.FragmentDefinition:
                    location = DirectiveLoc.FragmentDefinition;
                    return true;

                case NodeKind.FragmentSpread:
                    location = DirectiveLoc.FragmentSpread;
                    return true;

                case NodeKind.InlineFragment:
                    location = DirectiveLoc.InlineFragment;
                    return true;

                case NodeKind.OperationDefinition:
                    switch (((OperationDefinitionNode)node).Operation)
                    {
                        case OperationType.Query:
                            location = Types.DirectiveLocation.Query;
                            return true;

                        case OperationType.Mutation:
                            location = Types.DirectiveLocation.Mutation;
                            return true;

                        case OperationType.Subscription:
                            location = Types.DirectiveLocation.Subscription;
                            return true;

                        default:
                            location = default;
                            return false;
                    }

                default:
                    location = default;
                    return false;
            }
        }
    }
}
