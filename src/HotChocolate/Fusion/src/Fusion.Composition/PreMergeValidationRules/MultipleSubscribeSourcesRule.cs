using System.Collections.Immutable;
using System.Text;
using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.PreMergeValidationRules;

internal sealed class MultipleSubscribeSourcesRule : IEventHandler<OutputFieldGroupEvent>
{
    public void Handle(OutputFieldGroupEvent @event, CompositionContext context)
    {
        var fieldGroup = @event.FieldGroup;
        ImmutableArray<SubscribeContribution>.Builder? builder = null;

        foreach (var (field, _, schema) in fieldGroup)
        {
            foreach (var directive in field.GetSubscribeDirectives())
            {
                builder ??= ImmutableArray.CreateBuilder<SubscribeContribution>();
                builder.Add(new SubscribeContribution(field, schema, field.IsShareable, directive));
            }
        }

        var contributions = builder?.ToImmutable() ?? [];

        if (contributions.Length < 2 || contributions.Any(t => !t.IsShareable))
        {
            return;
        }

        for (var i = 1; i < contributions.Length; i++)
        {
            if (!TypeMergeHelper.SameTypeShape(
                contributions[0].Field.Type,
                contributions[i].Field.Type))
            {
                context.Log.Write(
                    OutputFieldTypesNotMergeable(
                        contributions[0].Field,
                        contributions[0].Schema,
                        contributions[i].Schema));
                return;
            }
        }

        var reference = SubscribeIdentity.Create(contributions[0].Directive);

        for (var i = 1; i < contributions.Length; i++)
        {
            if (!SubscribeIdentity.Create(contributions[i].Directive).Equals(reference))
            {
                context.Log.Write(MultipleSubscribeSources(contributions[0].Field, contributions[0].Schema));
                return;
            }
        }
    }

    private readonly record struct SubscribeContribution(
        MutableOutputFieldDefinition Field,
        MutableSchemaDefinition Schema,
        bool IsShareable,
        SubscribeDirectiveInfo Directive);

    private readonly record struct SubscribeIdentity(
        string? Broker,
        string Topics,
        string Message)
    {
        public static SubscribeIdentity Create(SubscribeDirectiveInfo directive)
        {
            return new SubscribeIdentity(
                directive.Broker,
                NormalizeTopics(directive.Topics),
                NormalizeSelectionSet(directive.Message));
        }
    }

    private static string NormalizeTopics(ImmutableArray<string> topics)
    {
        if (topics.IsDefaultOrEmpty)
        {
            return "";
        }

        var builder = new StringBuilder();
        var first = true;

        foreach (var topic in topics
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal))
        {
            if (!first)
            {
                builder.Append('\0');
            }

            builder.Append(topic);
            first = false;
        }

        return builder.ToString();
    }

    private static string NormalizeSelectionSet(SelectionSetNode selectionSet)
    {
        var builder = new StringBuilder();
        AppendSelectionSet(builder, selectionSet);
        return builder.ToString();
    }

    private static void AppendSelectionSet(StringBuilder builder, SelectionSetNode selectionSet)
    {
        var selections = selectionSet.Selections
            .Select(FormatSelection)
            .Order(StringComparer.Ordinal);
        var first = true;

        foreach (var selection in selections)
        {
            if (!first)
            {
                builder.Append(' ');
            }

            builder.Append(selection);
            first = false;
        }
    }

    private static string FormatSelection(ISelectionNode selection)
    {
        var builder = new StringBuilder();

        switch (selection)
        {
            case FieldNode field:
                if (field.Alias is not null)
                {
                    builder.Append(field.Alias.Value).Append(':');
                }

                builder.Append(field.Name.Value);

                if (field.SelectionSet is not null)
                {
                    builder.Append('{');
                    AppendSelectionSet(builder, field.SelectionSet);
                    builder.Append('}');
                }

                break;

            case InlineFragmentNode inlineFragment:
                builder.Append("...on ");
                builder.Append(inlineFragment.TypeCondition?.Name.Value);
                builder.Append('{');
                AppendSelectionSet(builder, inlineFragment.SelectionSet);
                builder.Append('}');
                break;

            case FragmentSpreadNode fragmentSpread:
                builder.Append("...");
                builder.Append(fragmentSpread.Name.Value);
                break;
        }

        return builder.ToString();
    }
}
