using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

public interface IDescriptorExtension : IHasDescriptorContext
{
    void OnBeforeCreate(Action<DefinitionBase> configure);

    void OnBeforeCreate(Action<IDescriptorContext, DefinitionBase> configure);

    INamedDependencyDescriptor OnBeforeNaming(
        Action<ITypeCompletionContext, DefinitionBase> configure);

    ICompletedDependencyDescriptor OnBeforeCompletion(
        Action<ITypeCompletionContext, DefinitionBase> configure);
}
