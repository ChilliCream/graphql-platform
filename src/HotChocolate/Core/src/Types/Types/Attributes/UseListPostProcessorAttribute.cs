using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public sealed class UseListPostProcessorAttribute<TElement>
    : UseResolverResultPostProcessorAttribute<ListPostProcessor<TElement>>
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
        => descriptor.ExtendWith(
            c => c.Definition.ResultPostProcessor = ListPostProcessor<TElement>.Default);
}
