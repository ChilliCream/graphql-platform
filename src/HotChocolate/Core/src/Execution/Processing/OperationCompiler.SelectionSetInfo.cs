using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal readonly struct SelectionSetInfo(SelectionSetNode selectionSet, long includeCondition)
    {
        public SelectionSetNode SelectionSet { get; } = selectionSet;

        public long IncludeCondition { get; } = includeCondition;
    }
}
