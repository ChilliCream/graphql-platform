namespace HotChocolate.Execution.Processing;

internal readonly struct OperationCompilerMetrics(int selections, int selectionSetVariants, int backlogMaxSize)
{
    public int Selections { get; } = selections;

    public int SelectionSetVariants { get; } = selectionSetVariants;

    public int BacklogMaxSize { get; } = backlogMaxSize;
}
