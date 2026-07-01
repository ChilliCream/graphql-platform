using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Validates that <c>@eventCursor</c> is used only where an event stream can supply or consume a
/// cursor.
/// </summary>
/// <remarks>
/// <para>
/// A cursor argument belongs on a subscription field with <c>@eventStream</c>. A cursor field belongs
/// directly on that subscription field's return type. Both markers must use <c>String</c> or
/// <c>String!</c>, and each subscription field can declare at most one cursor argument and one
/// cursor field.
/// </para>
/// <para>
/// A cursor argument requires a cursor field on the event payload type: the argument resumes the
/// stream from a position that the payload must be able to report. A cursor field without an
/// argument is valid, because the cursor can still be consumed as a message identifier.
/// </para>
/// <para>
/// Valid:
/// </para>
/// <code><![CDATA[
/// type Subscription {
///   onUserChanged(after: String @eventCursor): UserChangedEvent
///     @eventStream(message: "{ id changeType }")
/// }
///
/// type UserChangedEvent {
///   id: ID!
///   changeType: String!
///   cursor: String @eventCursor
/// }
/// ]]></code>
/// <para>
/// Invalid:
/// </para>
/// <code><![CDATA[
/// type Query {
///   users(after: String @eventCursor): [User!]!
/// }
///
/// type UserChangedEvent {
///   cursor: [String] @eventCursor
///   position: String @eventCursor
/// }
/// ]]></code>
/// </remarks>
internal sealed class EventCursorMarkerRule : IEventHandler<OutputFieldEvent>
{
    public void Handle(OutputFieldEvent @event, CompositionContext context)
    {
        var (field, declaringType, schema) = @event;
        var isSubscriptionRootField =
            schema.SubscriptionType == declaringType
            && field.GetEventStreamDirectives().Length > 0;

        if (isSubscriptionRootField)
        {
            var cursorArgumentCount = ValidateCursorArguments(field, schema, context);
            var cursorFieldCount = ValidateCursorFields(field, schema, context);

            if (cursorArgumentCount > 0 && cursorFieldCount == 0)
            {
                context.Log.Write(EventCursorArgumentRequiresCursorField(field, schema));
            }
        }
        else
        {
            foreach (var argument in field.Arguments.AsEnumerable())
            {
                if (argument.HasEventCursorDirective)
                {
                    context.Log.Write(EventCursorMarkerOnNonSubscriptionField(argument, schema));
                }
            }
        }

        if (field.HasEventCursorDirective
            && !IsReachableCursorField(field, declaringType, schema))
        {
            context.Log.Write(EventCursorMarkerOnNonSubscriptionField(field, schema));
        }
    }

    private static int ValidateCursorArguments(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        var cursorArgumentCount = 0;

        foreach (var argument in field.Arguments.AsEnumerable())
        {
            if (!argument.HasEventCursorDirective)
            {
                continue;
            }

            cursorArgumentCount++;

            if (!IsString(argument.Type))
            {
                context.Log.Write(EventCursorArgumentNotString(argument, schema));
            }
        }

        if (cursorArgumentCount > 1)
        {
            context.Log.Write(MultipleCursorArguments(field, schema));
        }

        return cursorArgumentCount;
    }

    private static int ValidateCursorFields(
        MutableOutputFieldDefinition field,
        MutableSchemaDefinition schema,
        CompositionContext context)
    {
        if (field.Type.AsTypeDefinition() is not MutableComplexTypeDefinition returnType)
        {
            return 0;
        }

        var cursorFieldCount = 0;

        foreach (var member in returnType.Fields.AsEnumerable())
        {
            if (!member.HasEventCursorDirective)
            {
                continue;
            }

            cursorFieldCount++;

            if (!IsString(member.Type))
            {
                context.Log.Write(EventCursorFieldNotString(member, schema));
            }
        }

        if (cursorFieldCount > 1)
        {
            context.Log.Write(MultipleCursorFields(field, schema));
        }

        return cursorFieldCount;
    }

    private static bool IsReachableCursorField(
        MutableOutputFieldDefinition field,
        ITypeDefinition declaringType,
        MutableSchemaDefinition schema)
    {
        if (schema.SubscriptionType is null)
        {
            return false;
        }

        foreach (var subscriptionField in schema.SubscriptionType.Fields.AsEnumerable())
        {
            if (subscriptionField.GetEventStreamDirectives().Length > 0
                && subscriptionField.Type.AsTypeDefinition() == declaringType
                && declaringType is MutableComplexTypeDefinition { Fields: var fields }
                && fields.Contains(field))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsString(IType type)
        => type.NullableType() is not ListType
            && type.AsTypeDefinition().Name == SpecScalarNames.String.Name;
}
