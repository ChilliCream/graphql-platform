using HotChocolate.Fusion.Events;
using HotChocolate.Fusion.Events.Contracts;
using HotChocolate.Fusion.Logging;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.Properties.CompositionResources;

namespace HotChocolate.Fusion.SourceSchemaValidationRules;

/// <summary>
/// Reports <c>@cost</c> applications whose source definition omits the required
/// <c>weight</c> argument.
/// </summary>
internal sealed class CostDirectiveDefinitionRule : IEventHandler<SchemaEvent>
{
    public void Handle(SchemaEvent @event, CompositionContext context)
    {
        var schema = @event.Schema;

        if (!schema.DirectiveDefinitions.TryGetDirective(
                WellKnownDirectiveNames.Cost,
                out var definition)
            || definition.Arguments.ContainsName(WellKnownArgumentNames.Weight))
        {
            return;
        }

        foreach (var member in GetMembers(schema))
        {
            if (member.Directives.FirstOrDefault(WellKnownDirectiveNames.Cost) is not null)
            {
                context.Log.Write(
                    LogEntryBuilder.New()
                        .SetMessage(CostDirective_WeightArgument_Invalid)
                        .SetCode(LogEntryCodes.InvalidGraphQL)
                        .SetSeverity(LogSeverity.Error)
                        .SetTypeSystemMember(member)
                        .SetSchema(schema)
                        .Build());
            }
        }
    }

    private static IEnumerable<IDirectivesProvider> GetMembers(MutableSchemaDefinition schema)
    {
        foreach (var type in schema.Types)
        {
            yield return type;

            switch (type)
            {
                case IComplexTypeDefinition complexType:
                    foreach (var field in complexType.Fields)
                    {
                        yield return field;

                        foreach (var argument in field.Arguments)
                        {
                            yield return argument;
                        }
                    }
                    break;

                case IEnumTypeDefinition enumType:
                    foreach (var value in enumType.Values)
                    {
                        yield return value;
                    }
                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    foreach (var field in inputObjectType.Fields)
                    {
                        yield return field;
                    }
                    break;
            }
        }
    }
}
