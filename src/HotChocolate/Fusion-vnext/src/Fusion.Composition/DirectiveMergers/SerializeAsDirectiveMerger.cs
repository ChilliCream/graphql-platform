using System.Collections.Immutable;
using System.Diagnostics;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Definitions;
using HotChocolate.Types.Mutable.Directives;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class SerializeAsDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.SerializeAs;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema)
    {
        return SerializeAsMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<IDirectivesProvider> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        if (MergeBehavior is DirectiveMergeBehavior.Ignore)
        {
            return;
        }

        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveName, out var directiveDefinition))
        {
            // Merged definition not found.
            return;
        }

        var serializeAsDirectives =
            memberDefinitions
                .SelectMany(d => d.Directives.Where(dir => dir.Name == DirectiveNames.SerializeAs))
                .Select(SerializeAsDirective.From)
                .ToArray();

        if (serializeAsDirectives.Length == 0)
        {
            return;
        }

        // All @serializeAs directives must have the same type and pattern.
        var firstDirective = serializeAsDirectives[0];
        if (!serializeAsDirectives.All(
            d => d.Type == firstDirective.Type && d.Pattern == firstDirective.Pattern))
        {
            return;
        }

        var argumentAssignments = new List<ArgumentAssignment>();
        var map = new Dictionary<ScalarSerializationType, EnumValueNode>();

        foreach (var possibleValue in Enum.GetValues<ScalarSerializationType>())
        {
            if (possibleValue is ScalarSerializationType.Undefined)
            {
                continue;
            }

            map.Add(possibleValue, new EnumValueNode(possibleValue.ToString().ToUpperInvariant()));
        }

        using var types = GetSetTypes(firstDirective.Type).GetEnumerator();

        IValueNode? typeArg = null;
        List<EnumValueNode>? listValue = null;

        while (types.MoveNext())
        {
            if (listValue is null && typeArg is null)
            {
                typeArg = map[types.Current];
            }
            else if (typeArg is not null && listValue is null)
            {
                listValue = [(EnumValueNode)typeArg, map[types.Current]];
                typeArg = null;
            }
            else
            {
                listValue?.Add(map[types.Current]);
            }
        }

        if (listValue is null && typeArg is null)
        {
            throw new InvalidOperationException("The @serializeAs directive has an invalid state.");
        }

        if (typeArg is null)
        {
            Debug.Assert(listValue is not null);
            typeArg = new ListValueNode(listValue);
        }

        argumentAssignments.Add(new ArgumentAssignment(ArgumentNames.Type, typeArg));

        if (firstDirective.Pattern is not null)
        {
            argumentAssignments.Add(
                new ArgumentAssignment(ArgumentNames.Pattern, firstDirective.Pattern));
        }

        var serializeAsDirective = new Directive(directiveDefinition, argumentAssignments);

        mergedMember.AddDirective(serializeAsDirective);
    }

    private static IEnumerable<ScalarSerializationType> GetSetTypes(ScalarSerializationType value)
    {
        var intValue = (int)value;
        for (var bit = 1; bit <= 32; bit <<= 1)
        {
            if ((intValue & bit) != 0)
            {
                yield return (ScalarSerializationType)bit;
            }
        }
    }
}
