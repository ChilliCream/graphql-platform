namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IEachOutputFieldEventHandler
{
    void OnEachOutputField(EachOutputFieldEvent @event);
}
