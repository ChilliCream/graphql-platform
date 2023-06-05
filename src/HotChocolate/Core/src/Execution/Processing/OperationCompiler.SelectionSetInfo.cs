using HotChocolate.Language;

namespace HotChocolate.Execution.Processing;

public sealed partial class OperationCompiler
{
    internal readonly struct SelectionSetInfo
    {
        public SelectionSetInfo(SelectionSetNode selectionSet, ulong includeConditionsMask)
        {
            SelectionSet = selectionSet;
            IncludeConditionsMask = includeConditionsMask;
        }

        public SelectionSetNode SelectionSet { get; }

        /// <see cref="OperationCompiler._includeConditions"/>
        public ulong IncludeConditionsMask { get; }
    }
}
