using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using static HotChocolate.Language.SyntaxKind;
using DirectiveLoc = HotChocolate.Types.DirectiveLocation;
using IHasDirectives = HotChocolate.Language.IHasDirectives;

namespace HotChocolate.Validation.Rules;

/// <summary>
/// GraphQL servers define what directives they support.
/// For each usage of a directive, the directive must be available
/// on that server.
///
/// https://spec.graphql.org/June2018/#sec-Directives-Are-Defined
///
/// AND
///
/// GraphQL servers define what directives they support and where they
/// support them.
///
/// For each usage of a directive, the directive must be used in a
/// location that the server has declared support for.
///
/// https://spec.graphql.org/June2018/#sec-Directives-Are-In-Valid-Locations
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
/// https://spec.graphql.org/draft/#sec-Directives-Are-Unique-Per-Location
///
/// AND
///
/// The @defer and @stream directives each accept an argument “label”.
/// This label may be used by GraphQL clients to uniquely identify response payloads.
/// If a label is passed, it must not be a variable, and it must be unique within
/// all other @defer and @stream directives in the document.
///
/// https://spec.graphql.org/draft/#sec-Defer-And-Stream-Directive-Labels-Are-Unique
/// </summary>
internal sealed class DirectiveVisitor()
    : DocumentValidatorVisitor(new SyntaxVisitorOptions { VisitDirectives = true })
{
    protected override ISyntaxVisitorAction Enter(
        ISyntaxNode node,
        DocumentValidatorContext context)
    {
        switch (node.Kind)
        {
            case Field:
            case SelectionSet:
            case InlineFragment:
            case FragmentSpread:
            case FragmentDefinition:
            case Directive:
            case VariableDefinition:
            case OperationDefinition:
            case Document:
                return base.Enter(node, context);

            default:
                return Skip;
        }
    }

    protected override ISyntaxVisitorAction Enter(
        DocumentNode node,
        DocumentValidatorContext context)
    {
        // The document node is the root node entered once per visitation.
        // We use this hook to ensure that the directive visitor feature is created,
        // and we can use it in consecutive visits of child nodes without extra
        // checks at each point.
        // We do use a GetOrSet here because the context is a pooled object.
        context.Features.GetOrSet<DirectiveVisitorFeature>();
        return base.Enter(node, context);
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        DocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        DocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        DocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        DocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        DocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        DocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        DirectiveNode node,
        DocumentValidatorContext context)
    {
        if (context.Schema.DirectiveDefinitions.TryGetDirective(node.Name.Value, out var dt))
        {
            if (context.Path.TryPeek(out var parent)
                && TryLookupLocation(parent, out var location)
                && (dt.Locations & location) != location)
            {
                context.ReportError(context.DirectiveNotValidInLocation(node));
            }
        }
        else
        {
            context.ReportError(context.DirectiveNotSupported(node));
        }
        return Skip;
    }

    private static void ValidateDirectives<T>(
        T node,
        DocumentValidatorContext context)
        where T : ISyntaxNode, IHasDirectives
    {
        var feature = context.Features.GetRequired<DirectiveVisitorFeature>();
        var directiveNames = feature.DirectiveNames;
        var labels = feature.Labels;
        directiveNames.Clear();

        foreach (var directive in node.Directives)
        {
            // ValidateDirectiveAreUniquePerLocation
            if (context.Schema.DirectiveDefinitions.TryGetDirective(directive.Name.Value, out var dt)
                && !dt.IsRepeatable
                && !directiveNames.Add(directive.Name.Value))
            {
                context.ReportError(context.DirectiveMustBeUniqueInLocation(directive));
            }

            // Defer And Stream Directive Labels Are Unique
            if (node.Kind is Field or InlineFragment or FragmentSpread
                && (directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal)
                || directive.Name.Value.Equals(DirectiveNames.Stream.Name, StringComparison.Ordinal)))
            {
                switch (directive.GetArgumentValue(DirectiveNames.Defer.Arguments.Label))
                {
                    case StringValueNode sn:
                        if (!labels.Add(sn.Value))
                        {
                            context.ReportError(context.DeferAndStreamDuplicateLabel(node, sn.Value));
                        }
                        break;

                    case VariableNode vn:
                        context.ReportError(context.DeferAndStreamLabelIsVariable(node, vn.Name.Value));
                        break;
                }
            }
        }
    }

    private static bool TryLookupLocation(ISyntaxNode node, out DirectiveLoc location)
    {
        switch (node.Kind)
        {
            case Field:
                location = DirectiveLoc.Field;
                return true;

            case FragmentDefinition:
                location = DirectiveLoc.FragmentDefinition;
                return true;

            case FragmentSpread:
                location = DirectiveLoc.FragmentSpread;
                return true;

            case InlineFragment:
                location = DirectiveLoc.InlineFragment;
                return true;

            case VariableDefinition:
                location = DirectiveLoc.VariableDefinition;
                return true;

            case OperationDefinition:
                switch (((OperationDefinitionNode)node).Operation)
                {
                    case OperationType.Query:
                        location = DirectiveLoc.Query;
                        return true;

                    case OperationType.Mutation:
                        location = DirectiveLoc.Mutation;
                        return true;

                    case OperationType.Subscription:
                        location = DirectiveLoc.Subscription;
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

    private sealed class DirectiveVisitorFeature : ValidatorFeature
    {
        public HashSet<string> DirectiveNames { get; } = [];

        public HashSet<string> Labels { get; } = [];

        protected internal override void Reset()
        {
            DirectiveNames.Clear();
            Labels.Clear();
        }
    }
}
