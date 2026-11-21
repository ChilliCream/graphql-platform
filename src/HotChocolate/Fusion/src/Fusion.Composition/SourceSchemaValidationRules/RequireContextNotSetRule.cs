using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Logging.LogEntryHelper;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Every context name that the <c>@require</c> directive of an argument references must be the
/// name of an <c>@context</c> directive on a type in the same source schema. Each missing name
/// raises a <c>REQUIRE_CONTEXT_NOT_SET</c> error.
/// </summary>
internal sealed class RequireContextNotSetRule : IEventHandler<FieldArgumentEvent>
{
    public void Handle(FieldArgumentEvent @event, CompositionContext context)
    {
        var (argument, _, _, schema) = @event;

        if (argument.RequireInfo is not { ContextNames.Length: > 0 } requireInfo)
        {
            return;
        }

        foreach (var contextName in requireInfo.ContextNames)
        {
            if (!IsContextSet(schema, contextName))
            {
                context.Log.Write(
                    RequireContextNotSet(requireInfo.Directive, argument, contextName, schema));
            }
        }
    }

    private static bool IsContextSet(MutableSchemaDefinition schema, string contextName)
    {
        foreach (var type in schema.Types)
        {
            if (type is not (MutableObjectTypeDefinition
                    or MutableInterfaceTypeDefinition
                    or MutableUnionTypeDefinition)
                || type is not IDirectivesProvider provider)
            {
                continue;
            }

            foreach (var directive in provider.Directives.AsEnumerable())
            {
                if (directive.Name == DirectiveNames.Context
                    && directive.Arguments.TryGetValue(ArgumentNames.Name, out var value)
                    && value is StringValueNode { Value: var name }
                    && name == contextName)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
