using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// The result of splitting an operation at @defer boundaries: a stripped
/// main operation plus one subplan descriptor per unique
/// <see cref="DeferUsage"/> set.
/// </summary>
internal readonly record struct DeferSplitResult(
    OperationDefinitionNode MainOperation,
    ImmutableArray<DeferSubPlanDescriptor> SubPlanDescriptors);
