namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IEachOutputFieldNameEventHandler
{
    void OnEachOutputFieldName(EachOutputFieldNameEvent @event);
}
