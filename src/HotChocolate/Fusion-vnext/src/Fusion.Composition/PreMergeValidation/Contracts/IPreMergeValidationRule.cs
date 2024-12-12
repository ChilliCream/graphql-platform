namespace HotChocolate.Fusion.PreMergeValidation.Contracts;

internal interface IPreMergeValidationRule
{
    void OnEachType(EachTypeEvent @event);

    void OnEachOutputField(EachOutputFieldEvent @event);

    void OnEachFieldArgument(EachFieldArgumentEvent @event);

    void OnEachDirective(EachDirectiveEvent @event);

    void OnEachDirectiveArgument(EachDirectiveArgumentEvent @event);

    void OnEachTypeGroup(EachTypeGroupEvent @event);

    void OnEachOutputFieldGroup(EachOutputFieldGroupEvent @event);

    void OnEachFieldArgumentGroup(EachFieldArgumentGroupEvent @event);
}
