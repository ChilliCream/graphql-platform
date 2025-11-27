using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal readonly struct SelectionSetInfo(
        SelectionSetNode selectionSet,
        ulong includeCondition)
    {
        public SelectionSetNode SelectionSet { get; } = selectionSet;

        public ulong IncludeCondition { get; } = includeCondition;
    }
}
