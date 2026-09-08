using System.Collections.Immutable;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// The <c>@listSize</c> metadata of one output field, with every default
/// already resolved by the snapshot builder.
/// </summary>
/// <param name="AssumedSize">
/// The <c>assumedSize</c> argument, or <see langword="null"/> when absent.
/// </param>
/// <param name="SlicingArguments">
/// The <c>slicingArguments</c> argument's names, empty when absent.
/// </param>
/// <param name="SlicingArgumentDefaultValue">
/// The <c>slicingArgumentDefaultValue</c> argument, or <see langword="null"/>
/// when absent.
/// </param>
/// <param name="SizedFields">
/// The <c>sizedFields</c> argument's names, empty when absent.
/// </param>
/// <param name="RequireOneSlicingArgument">
/// The effective <c>requireOneSlicingArgument</c> value: the usage's own
/// literal when present, else the directive definition's declared default,
/// else the spec default <see langword="true"/>.
/// </param>
internal sealed record ListSizeMetadata(
    double? AssumedSize,
    ImmutableArray<string> SlicingArguments,
    double? SlicingArgumentDefaultValue,
    ImmutableArray<string> SizedFields,
    bool RequireOneSlicingArgument);
