using System;
using HotChocolate.Configuration;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        IServiceProvider Services { get; }

        IReadOnlySchemaOptions Options { get; }

        INamingConventions Naming { get; }

        ITypeInspector Inspector { get; }
    }
}
