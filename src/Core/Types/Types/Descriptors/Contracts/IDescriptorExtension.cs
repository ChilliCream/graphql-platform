using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDescriptorExtension<T>
        where T : DefinitionBase
    {
        void OnBeforeCreate(Action<T> configure);

        IDependencyDescriptor OnBeforeNaming(
            Action<ICompletionContext, T> configure);

        IDependencyDescriptor OnBeforeCompletion(
            Action<ICompletionContext, T> configure);
    }
}
