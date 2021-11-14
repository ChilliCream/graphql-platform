namespace HotChocolate.Execution.Processing;

internal interface IHasResultDataParent
{
    IResultData? Parent { get; set; }
}
