namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IEachTypeEventHandler
{
    void OnEachType(EachTypeEvent @event);
}
