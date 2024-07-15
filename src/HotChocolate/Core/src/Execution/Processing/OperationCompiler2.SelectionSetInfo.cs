using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationCompiler2
{
    internal readonly struct SelectionSetInfo(SelectionSetNode selectionSet, long includeCondition)
    {
        public SelectionSetNode SelectionSet { get; } = selectionSet;

        public long IncludeCondition { get; } = includeCondition;
    }
}
