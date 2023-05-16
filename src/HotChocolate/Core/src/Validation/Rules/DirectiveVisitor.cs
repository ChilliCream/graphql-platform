using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;
using HotChocolate.Utilities;
using static HotChocolate.Language.SyntaxKind;
using DirectiveLoc = HotChocolate.Types.DirectiveLocation;
using IHasDirectives = HotChocolate.Language.IHasDirectives;

namespace HotChocolate.Validation.Rules;

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
///
/// AND
///
/// The @defer and @stream directives each accept an argument “label”.
/// This label may be used by GraphQL clients to uniquely identify response payloads.
/// If a label is passed, it must not be a variable and it must be unique within
/// all other @defer and @stream directives in the document.
///
/// http://spec.graphql.org/draft/#sec-Defer-And-Stream-Directive-Labels-Are-Unique
/// </summary>
internal sealed class DirectiveVisitor : DocumentValidatorVisitor
{
    public DirectiveVisitor()
        : base(new SyntaxVisitorOptions
        {
            VisitDirectives = true
        })
    {
    }

    protected override ISyntaxVisitorAction Enter(
        ISyntaxNode node,
        IDocumentValidatorContext context)
    {
        switch (node.Kind)
        {
            case Field:
            case SelectionSet:
            case InlineFragment:
            case FragmentSpread:
            case FragmentDefinition:
            case SyntaxKind.Directive:
            case VariableDefinition:
            case OperationDefinition:
            case Document:
                return base.Enter(node, context);

            default:
                return Skip;
        }
    }

    protected override ISyntaxVisitorAction Enter(
        OperationDefinitionNode node,
        IDocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        VariableDefinitionNode node,
        IDocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FieldNode node,
        IDocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        InlineFragmentNode node,
        IDocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentDefinitionNode node,
        IDocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        FragmentSpreadNode node,
        IDocumentValidatorContext context)
    {
        ValidateDirectives(node, context);
        return Continue;
    }

    protected override ISyntaxVisitorAction Enter(
        DirectiveNode node,
        IDocumentValidatorContext context)
    {
        if (context.Schema.TryGetDirectiveType(node.Name.Value, out var dt))
        {
            if (context.Path.TryPeek(out var parent) &&
                TryLookupLocation(parent, out var location) &&
                (dt.Locations & location) != location)
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
        IDocumentValidatorContext context)
        where T : ISyntaxNode, IHasDirectives
    {
        context.Names.Clear();
        foreach (var directive in node.Directives)
        {
            // ValidateDirectiveAreUniquePerLocation
            if (context.Schema.TryGetDirectiveType(directive.Name.Value, out var dt)
                && !dt.IsRepeatable
                && !context.Names.Add(directive.Name.Value))
            {
                context.ReportError(context.DirectiveMustBeUniqueInLocation(directive));
            }

            // Defer And Stream Directive Labels Are Unique
            if (node.Kind is Field or InlineFragment or FragmentSpread &&
                (directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Defer) ||
                directive.Name.Value.EqualsOrdinal(WellKnownDirectives.Stream)))
            {
                if (directive.GetLabelArgumentValueOrDefault() is StringValueNode sn)
                {
                    if (!context.Declared.Add(sn.Value))
                    {
                        context.ReportError(context.DeferAndStreamDuplicateLabel(node, sn.Value));
                    }
                }
                else if (directive.GetLabelArgumentValueOrDefault() is VariableNode vn)
                {
                    context.ReportError(context.DeferAndStreamLabelIsVariable(node, vn.Name.Value));
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
