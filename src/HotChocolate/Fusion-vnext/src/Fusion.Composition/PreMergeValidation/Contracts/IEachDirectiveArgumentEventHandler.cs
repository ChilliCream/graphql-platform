namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IEachDirectiveArgumentEventHandler
{
    void OnEachDirectiveArgument(EachDirectiveArgumentEvent @event);
}
