using System;

namespace HotChocolate.Data.Filters
{
    public interface IConventionContext
    {
        string? Scope { get; }
        IServiceProvider Services { get; }
    }
}
