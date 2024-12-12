namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IEachFieldArgumentEventHandler
{
    void OnEachFieldArgument(EachFieldArgumentEvent @event);
}
