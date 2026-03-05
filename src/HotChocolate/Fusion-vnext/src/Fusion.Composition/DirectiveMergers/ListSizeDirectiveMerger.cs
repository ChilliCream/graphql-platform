using System.Collections.Immutable;
using HotChocolate.Fusion.Definitions;
using HotChocolate.Fusion.Directives;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Info;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;
using DirectiveNames = HotChocolate.Fusion.WellKnownDirectiveNames;

namespace HotChocolate.Fusion.DirectiveMergers;

internal class ListSizeDirectiveMerger(DirectiveMergeBehavior mergeBehavior)
    : DirectiveMergerBase(mergeBehavior)
{
    public override string DirectiveName => DirectiveNames.ListSize;

    public override MutableDirectiveDefinition GetCanonicalDirectiveDefinition(ISchemaDefinition schema)
    {
        return ListSizeMutableDirectiveDefinition.Create(schema);
    }

    public override void MergeDirectives(
        IDirectivesProvider mergedMember,
        ImmutableArray<DirectivesProviderInfo> memberDefinitions,
        MutableSchemaDefinition mergedSchema)
    {
        if (!mergedSchema.DirectiveDefinitions.TryGetDirective(DirectiveName, out var directiveDefinition))
        {
            // Merged definition not found.
            return;
        }

        var listSizeDirectives =
            memberDefinitions
                .SelectMany(d => d.Member.Directives.Where(dir => dir.Name == DirectiveNames.ListSize))
                .Select(ListSizeDirective.From)
                .ToArray();

        if (listSizeDirectives.Length == 0)
        {
            return;
        }

        var argumentAssignments = new List<ArgumentAssignment>();

        var maxAssumedSize = listSizeDirectives.Max(d => d.AssumedSize);

        if (maxAssumedSize is not null)
        {
            var assumedSizeArgument =
                new ArgumentAssignment(ArgumentNames.AssumedSize, new IntValueNode(maxAssumedSize.Value));

            argumentAssignments.Add(assumedSizeArgument);
        }

        var allSlicingArguments = listSizeDirectives
            .SelectMany(d => d.SlicingArguments)
            .Distinct()
            .ToImmutableArray();

        if (allSlicingArguments.Length != 0)
        {
            var slicingArgumentsArgument =
                new ArgumentAssignment(
                    ArgumentNames.SlicingArguments,
                    new ListValueNode(allSlicingArguments.Select(a => new StringValueNode(a)).ToList()));

            argumentAssignments.Add(slicingArgumentsArgument);
        }

        var allSizedFields = listSizeDirectives
            .SelectMany(d => d.SizedFields)
            .Distinct()
            .ToImmutableArray();

        if (allSizedFields.Length != 0)
        {
            var sizedFieldsArgument =
                new ArgumentAssignment(
                    ArgumentNames.SizedFields,
                    new ListValueNode(allSizedFields.Select(f => new StringValueNode(f)).ToList()));

            argumentAssignments.Add(sizedFieldsArgument);
        }

        var requireOneSlicingArgument = CombineBooleans(
            listSizeDirectives.Select(d => d.RequireOneSlicingArgument));

        if (requireOneSlicingArgument is not null)
        {
            var requireOneSlicingArgumentArgument =
                new ArgumentAssignment(
                    ArgumentNames.RequireOneSlicingArgument,
                    new BooleanValueNode(requireOneSlicingArgument.Value));

            argumentAssignments.Add(requireOneSlicingArgumentArgument);
        }

        var maxSlicingArgumentDefaultValue = listSizeDirectives.Max(d => d.SlicingArgumentDefaultValue);

        if (maxSlicingArgumentDefaultValue is not null)
        {
            var slicingArgumentDefaultValueArgument =
                new ArgumentAssignment(
                    ArgumentNames.SlicingArgumentDefaultValue,
                    new IntValueNode(maxSlicingArgumentDefaultValue.Value));

            argumentAssignments.Add(slicingArgumentDefaultValueArgument);
        }

        var listSizeDirective = new Directive(directiveDefinition, argumentAssignments);

        mergedMember.AddDirective(listSizeDirective);
    }

    /// <summary>
    /// Combines a sequence of nullable boolean values using specific aggregation rules.
    /// </summary>
    /// <param name="values">The sequence of nullable boolean values to combine.</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description><c>true</c> if any value is <c>true</c> (mixed values case)</description></item>
    /// <item><description><c>false</c> if all values are <c>false</c></description></item>
    /// <item><description><c>null</c> if all values are <c>null</c> or the sequence is empty</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This method performs a single-pass evaluation with early exit optimization.
    /// When the first <c>true</c> value is encountered, the method returns immediately.
    /// </remarks>
    private static bool? CombineBooleans(IEnumerable<bool?> values)
    {
        bool? result = null;

        foreach (var value in values)
        {
            switch (value)
            {
                case true:
                    return true; // Early exit - we know it's not "all false" or "all null".
                case false:
                    result = false;
                    break;
            }
        }

        return result;
    }
}
