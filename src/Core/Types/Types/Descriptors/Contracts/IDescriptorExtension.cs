using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    public interface IDescriptorExtension
    {
        void OnBeforeCreate(Action<DefinitionBase> configure);

        INamedDependencyDescriptor OnBeforeNaming(
            Action<ICompletionContext, DefinitionBase> configure);

        ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ICompletionContext, DefinitionBase> configure);
    }
}
