using System.Diagnostics;

#nullable enable

namespace HotChocolate.Types.Descriptors;

public abstract class Convention<TDefinition> : Convention
    where TDefinition : class
{
    private TDefinition? _definition;

    protected virtual TDefinition? Definition
    {
        get
        {
            return _definition;
        }
    }

    protected internal sealed override void Initialize(IConventionContext context)
    {
        AssertUninitialized();

        Scope = context.Scope;
        _definition = CreateDefinition(context);
        MarkInitialized();
    }

    protected internal override void Complete(IConventionContext context)
    {
        _definition = null;
    }

    protected abstract TDefinition CreateDefinition(IConventionContext context);

    private void AssertUninitialized()
    {
        Debug.Assert(
            !IsInitialized,
            "The type must be uninitialized.");

        Debug.Assert(
            _definition is null,
            "The definition should not exist when the type has not been initialized.");

        if (IsInitialized || _definition is not null)
        {
            throw new InvalidOperationException();
        }
    }
}
