using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.Fusion.Properties.CompositionResources;
using static HotChocolate.Types.DirectiveNames;

namespace HotChocolate.Fusion.Directives;

internal sealed class ListSizeDirective(
    int? assumedSize = null,
    ImmutableArray<string>? slicingArguments = null,
    ImmutableArray<string>? sizedFields = null,
    bool? requireOneSlicingArgument = null,
    int? slicingArgumentDefaultValue = null)
{
    public int? AssumedSize { get; } = assumedSize;

    public ImmutableArray<string> SlicingArguments { get; } = slicingArguments ?? [];

    public int? SlicingArgumentDefaultValue { get; } = slicingArgumentDefaultValue;

    public ImmutableArray<string> SizedFields { get; } = sizedFields ?? [];

    public bool? RequireOneSlicingArgument { get; } = requireOneSlicingArgument;

    public static ListSizeDirective From(IDirective directive)
    {
        int? assumedSize = null;
        ImmutableArray<string>? slicingArguments = null;
        ImmutableArray<string>? sizedFields = null;
        bool? requireOneSlicingArgument = null;
        int? slicingArgumentDefaultValue = null;

        if (directive.Arguments.TryGetValue(ListSize.Arguments.AssumedSize, out var assumedSizeArg))
        {
            assumedSize = assumedSizeArg switch
            {
                IntValueNode intValueNode => intValueNode.ToInt32(),
                NullValueNode => null,
                _ => throw new InvalidOperationException(ListSizeDirective_AssumedSizeArgument_Invalid)
            };
        }

        if (directive.Arguments.TryGetValue(ListSize.Arguments.SlicingArguments, out var slicingArgumentsArg))
        {
            slicingArguments = slicingArgumentsArg switch
            {
                ListValueNode listValueNode when listValueNode.Items.All(v => v is StringValueNode)
                    => listValueNode.Items.Cast<StringValueNode>().Select(v => v.Value).ToImmutableArray(),
                NullValueNode => [],
                _ => throw new InvalidOperationException(ListSizeDirective_SlicingArgumentsArgument_Invalid)
            };
        }

        if (directive.Arguments.TryGetValue(ListSize.Arguments.SizedFields, out var sizedFieldsArg))
        {
            sizedFields = sizedFieldsArg switch
            {
                ListValueNode listValueNode when listValueNode.Items.All(v => v is StringValueNode)
                    => listValueNode.Items.Cast<StringValueNode>().Select(v => v.Value).ToImmutableArray(),
                NullValueNode => [],
                _ => throw new InvalidOperationException(ListSizeDirective_SizedFieldsArgument_Invalid)
            };
        }

        if (
            directive.Arguments.TryGetValue(
                ListSize.Arguments.RequireOneSlicingArgument,
                out var requireOneSlicingArgumentArg))
        {
            requireOneSlicingArgument = requireOneSlicingArgumentArg switch
            {
                BooleanValueNode booleanValueNode => booleanValueNode.Value,
                NullValueNode => null,
                _ => throw new InvalidOperationException(ListSizeDirective_RequireOneSlicingArgumentArgument_Invalid)
            };
        }

        if (
            directive.Arguments.TryGetValue(
                ListSize.Arguments.SlicingArgumentDefaultValue,
                out var slicingArgumentDefaultValueArg))
        {
            slicingArgumentDefaultValue = slicingArgumentDefaultValueArg switch
            {
                IntValueNode intValueNode => intValueNode.ToInt32(),
                NullValueNode => null,
                _ => throw new InvalidOperationException(ListSizeDirective_SlicingArgumentDefaultValueArgument_Invalid)
            };
        }

        return new ListSizeDirective(
            assumedSize,
            slicingArguments,
            sizedFields,
            requireOneSlicingArgument,
            slicingArgumentDefaultValue);
    }
}
