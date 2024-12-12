namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IEachDirectiveEventHandler
{
    void OnEachDirective(EachDirectiveEvent @event);
}
