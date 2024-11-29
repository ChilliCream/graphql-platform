using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

/// <summary>
/// Allows for access to the type definition.
/// </summary>
/// <typeparam name="T">The type definition.</typeparam>
public interface IDescriptorExtension<out T> : IHasDescriptorContext
    where T : DefinitionBase
{
    /// <summary>
    /// The type definition.
    /// </summary>
    T Definition { get; }

    /// <summary>
    /// Allows to rewrite the type definition before the type
    /// is created but after all the users descriptor changes
    /// are applied.
    /// </summary>
    void OnBeforeCreate(Action<T> configure);

    /// <summary>
    /// Allows to rewrite the type definition before the type
    /// is created but after all the users descriptor changes
    /// are applied.
    /// </summary>
    void OnBeforeCreate(Action<IDescriptorContext, T> configure);

    /// <summary>
    /// Allows to rewrite the type definition before the type
    /// name is applied but after
    /// <see cref="OnBeforeCreate(Action{IDescriptorContext, T})"/>.
    /// </summary>
    INamedDependencyDescriptor OnBeforeNaming(
        Action<ITypeCompletionContext, T> configure);

    /// <summary>
    /// Allows to rewrite the type definition before the type
    /// is completed but after
    /// <see cref="OnBeforeNaming(Action{ITypeCompletionContext, T})"/>.
    /// </summary>
    ICompletedDependencyDescriptor OnBeforeCompletion(
        Action<ITypeCompletionContext, T> configure);
}
