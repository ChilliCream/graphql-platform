using HotChocolate.Fusion.PreMergeValidation.Contracts;

namespace HotChocolate.Fusion.PreMergeValidation;

internal abstract class PreMergeValidationRule : IPreMergeValidationRule
{
    public virtual void OnEachType(EachTypeEvent @event)
    {
    }

    public virtual void OnEachOutputField(EachOutputFieldEvent @event)
    {
    }

    public virtual void OnEachFieldArgument(EachFieldArgumentEvent @event)
    {
    }

    public virtual void OnEachDirective(EachDirectiveEvent @event)
    {
    }

    public virtual void OnEachDirectiveArgument(EachDirectiveArgumentEvent @event)
    {
    }

    public virtual void OnEachTypeGroup(EachTypeGroupEvent @event)
    {
    }

    public virtual void OnEachOutputFieldGroup(EachOutputFieldGroupEvent @event)
    {
    }

    public virtual void OnEachFieldArgumentGroup(EachFieldArgumentGroupEvent @event)
    {
    }
}
