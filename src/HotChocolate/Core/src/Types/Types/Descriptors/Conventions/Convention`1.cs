using System.Diagnostics;

namespace HotChocolate.Types.Descriptors;

public abstract class Convention<TConfiguration> : Convention where TConfiguration : class
{
    private TConfiguration? _configuration;

    protected virtual TConfiguration? Configuration
    {
        get
        {
            return _configuration;
        }
    }

    protected internal sealed override void Initialize(IConventionContext context)
    {
        AssertUninitialized();

        Scope = context.Scope;
        _configuration = CreateConfiguration(context);
        MarkInitialized();
    }

    protected internal override void Complete(IConventionContext context)
    {
        _configuration = null;
    }

    protected abstract TConfiguration CreateConfiguration(IConventionContext context);

    private void AssertUninitialized()
    {
        Debug.Assert(
            !IsInitialized,
            "The type must be uninitialized.");

        Debug.Assert(
            _configuration is null,
            "The definition should not exist when the type has not been initialized.");

        if (IsInitialized || _configuration is not null)
        {
            throw new InvalidOperationException();
        }
    }
}
