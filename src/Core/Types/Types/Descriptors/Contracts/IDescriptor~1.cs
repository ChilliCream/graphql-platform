using System;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptor<T>
        where T : DefinitionBase
    {
        void Configure(Action<T> definitionModifier);
    }
}
