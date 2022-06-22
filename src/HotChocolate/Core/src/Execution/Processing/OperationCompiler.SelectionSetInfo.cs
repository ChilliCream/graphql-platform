using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal readonly struct SelectionSetInfo
    {
        public SelectionSetInfo(SelectionSetNode selectionSet, long includeCondition)
        {
            SelectionSet = selectionSet;
            IncludeCondition = includeCondition;
        }

        public SelectionSetNode SelectionSet { get; }

        public long IncludeCondition { get; }
    }
}
