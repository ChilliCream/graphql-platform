using System;

#nullable enable

namespace HotChocolate.Types.Descriptors
{
    public interface IConventionContext 
    {
        string? Scope { get; }

        IServiceProvider Services { get; }

        IDescriptorContext DescriptorContext { get; }
    }
}
