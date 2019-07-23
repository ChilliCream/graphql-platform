using System;
using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types
{
    public interface IDescriptorExtension<T>
        where T : DefinitionBase
    {
        void OnBeforeCreate(Action<T> configure);

        INamedDependencyDescriptor OnBeforeNaming(
            Action<ICompletionContext, T> configure);

        ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ICompletionContext, T> configure);
    }
}
