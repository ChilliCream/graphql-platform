using System;

namespace HotChocolate.Execution.Processing;

internal sealed partial class OperationContext
{
    /// <summary>
    /// Gets the request scoped services
    /// </summary>
    public IServiceProvider Services
    {
        get
        {
            AssertInitialized();
            return _services;
        }
    }
    
    /// <summary>
    /// Gets the activator helper class.
    /// </summary>
    public IActivator Activator
    {
        get
        {
            AssertInitialized();
            return _activator;
        }
    }
}
