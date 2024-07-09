using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Types;

public class UseResolverResultPostProcessorAttribute<T>
    : ObjectFieldDescriptorAttribute
    where T : class, IResolverResultPostProcessor
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        var services = context.Services.GetRequiredService<IApplicationServiceProvider>();
        var postProcessor = ActivatorUtilities.GetServiceOrCreateInstance<T>(services);
        descriptor.ExtendWith(c => c.Definition.ResultPostProcessor = postProcessor);
    }
}
