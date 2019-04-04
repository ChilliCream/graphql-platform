using System;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDescriptor<T>
        where T : DefinitionBase
    {
        void Configure(Action<T> configure);
        void Configure(TypeConfiguration<T> configuration);
    }
}
