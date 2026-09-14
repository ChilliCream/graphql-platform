using HotChocolate.Language;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// One parent member's <c>@listSize(sizedFields:)</c> metadata and slicing
/// arguments, resolved only when a compiled plan is evaluated.
/// </summary>
/// <param name="Metadata">
/// The parent's list-size metadata.
/// </param>
/// <param name="SlicingArguments">
/// The parent's slicing arguments.
/// </param>
/// <param name="DefaultListSize">
/// The snapshot's default list size.
/// </param>
internal readonly record struct SizedFieldContext(
    ListSizeMetadata Metadata,
    IReadOnlyDictionary<string, SlicingArgumentValue> SlicingArguments,
    double DefaultListSize)
{
    public bool DependsOnVariables
    {
        get
        {
            foreach (var argument in SlicingArguments.Values)
            {
                if (argument.SuppliedValue is VariableNode)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public bool TryResolve(
        string fieldName,
        ICostVariableValues? variableValues,
        out double size)
    {
        if (!Metadata.SizedFields.Contains(fieldName, StringComparer.Ordinal))
        {
            size = 0.0;
            return false;
        }

        return ListSizeResolver.TryResolveSizedFieldSize(
            Metadata,
            SlicingArguments,
            variableValues,
            DefaultListSize,
            out size);
    }
}
