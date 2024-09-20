using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Raven;

public class CatchErrorMiddlewareAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Use(next => async context =>
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                context.ContextData["ex"] = ex.Message;
            }
        });
    }
}
