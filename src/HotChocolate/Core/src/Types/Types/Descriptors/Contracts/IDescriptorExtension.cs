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
            Action<ITypeCompletionContext, DefinitionBase> configure);

        ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ITypeCompletionContext, DefinitionBase> configure);
    }
}
