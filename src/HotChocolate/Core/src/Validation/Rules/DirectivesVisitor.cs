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
    ///
    /// AND
    ///
    /// Directives are used to describe some metadata or behavioral change on
    /// the definition they apply to.
    ///
    /// When more than one directive of the
    /// same name is used, the expected metadata or behavior becomes ambiguous,
    /// therefore only one of each directive is allowed per location.
    ///
    /// http://spec.graphql.org/draft/#sec-Directives-Are-Unique-Per-Location
    /// </summary>
    internal sealed class DirectivesVisitor
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
                case NodeKind.InlineFragment:
                case NodeKind.FragmentSpread:
                case NodeKind.FragmentDefinition:
                case NodeKind.Directive:
                case NodeKind.VariableDefinition:
                case NodeKind.OperationDefinition:
                case NodeKind.Document:
                    return base.Enter(node, context);

                default:
                    return Skip;
            }
        }

        protected override ISyntaxVisitorAction Enter(
            OperationDefinitionNode node,
            IDocumentValidatorContext context)
        {
            ValidateDirectiveAreUniquePerLocation(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            VariableDefinitionNode node,
            IDocumentValidatorContext context)
        {
            ValidateDirectiveAreUniquePerLocation(node, context);
            return Skip;
        }

        protected override ISyntaxVisitorAction Enter(
            FieldNode node,
            IDocumentValidatorContext context)
        {
            ValidateDirectiveAreUniquePerLocation(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            InlineFragmentNode node,
            IDocumentValidatorContext context)
        {
            ValidateDirectiveAreUniquePerLocation(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentDefinitionNode node,
            IDocumentValidatorContext context)
        {
            ValidateDirectiveAreUniquePerLocation(node, context);
            return Continue;
        }

        protected override ISyntaxVisitorAction Enter(
            FragmentSpreadNode node,
            IDocumentValidatorContext context)
        {
            ValidateDirectiveAreUniquePerLocation(node, context);
            return Continue;
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

        private void ValidateDirectiveAreUniquePerLocation<T>(
            T node,
            IDocumentValidatorContext context)
            where T : ISyntaxNode, Language.IHasDirectives
        {
            context.Names.Clear();

            foreach (DirectiveNode directive in node.Directives)
            {
                if (context.Schema.TryGetDirectiveType(directive.Name.Value, out DirectiveType? dt)
                    && !dt.IsRepeatable
                    && !context.Names.Add(directive.Name.Value))
                {
                    context.Errors.Add(
                        ErrorBuilder.New()
                            .SetMessage("Only one of each directive is allowed per location.")
                            .AddLocation(node)
                            .SetPath(context.CreateErrorPath())
                            .SpecifiedBy("sec-Directives-Are-Unique-Per-Location")
                            .Build());
                }
            }
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

                case NodeKind.VariableDefinition:
                    location = DirectiveLoc.VariableDefinition;
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
