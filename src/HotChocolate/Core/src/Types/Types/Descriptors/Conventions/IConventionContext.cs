using System;

namespace HotChocolate.Types.Descriptors
{
    public interface IConventionContext
    {
        string? Scope { get; }

        IServiceProvider Services { get; }
    }
}
