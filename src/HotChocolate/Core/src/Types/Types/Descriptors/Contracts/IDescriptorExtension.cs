using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Types;

public interface IDescriptorExtension : IHasDescriptorContext
{
    void OnBeforeCreate(Action<TypeSystemConfiguration> configure);

    void OnBeforeCreate(Action<IDescriptorContext, TypeSystemConfiguration> configure);

    INamedDependencyDescriptor OnBeforeNaming(
        Action<ITypeCompletionContext, TypeSystemConfiguration> configure);

    ICompletedDependencyDescriptor OnBeforeCompletion(
        Action<ITypeCompletionContext, TypeSystemConfiguration> configure);
}
