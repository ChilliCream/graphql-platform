using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public interface IDescriptorExtension<T>
        where T : DefinitionBase
    {
        void OnBeforeCreate(Action<T> configure);

        void OnBeforeCreate(Action<IDescriptorContext, T> configure);

        INamedDependencyDescriptor OnBeforeNaming(
            Action<ITypeCompletionContext, T> configure);

        ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ITypeCompletionContext, T> configure);
    }
}
