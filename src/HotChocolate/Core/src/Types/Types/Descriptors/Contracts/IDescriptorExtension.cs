using System;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    /// <summary>
    /// Allows for access to the type definition.
    /// </summary>
    public interface IDescriptorExtension : IHasDescriptorContext
    {
        /// <summary>
        /// The type definition.
        /// </summary>
        DefinitionBase Definition { get; }

        /// <summary>
        /// Allows to rewrite the type definition before the type
        /// is created but after all the users descriptor changes
        /// are applied.
        /// </summary>
        void OnBeforeCreate(Action<DefinitionBase> configure);

        /// <summary>
        /// Allows to rewrite the type definition before the type
        /// is created but after all the users descriptor changes
        /// are applied.
        /// </summary>
        void OnBeforeCreate(Action<IDescriptorContext, DefinitionBase> configure);

        /// <summary>
        /// Allows to rewrite the type definition before the type
        /// name is applied but after
        /// <see cref="OnBeforeCreate(Action{IDescriptorContext, DefinitionBase})"/>.
        /// </summary>
        INamedDependencyDescriptor OnBeforeNaming(
            Action<ITypeCompletionContext, DefinitionBase> configure);

        /// <summary>
        /// Allows to rewrite the type definition before the type
        /// is completed but after
        /// <see cref="OnBeforeCompletion(Action{ITypeCompletionContext, DefinitionBase})"/>.
        /// </summary>
        ICompletedDependencyDescriptor OnBeforeCompletion(
            Action<ITypeCompletionContext, DefinitionBase> configure);
    }
}
