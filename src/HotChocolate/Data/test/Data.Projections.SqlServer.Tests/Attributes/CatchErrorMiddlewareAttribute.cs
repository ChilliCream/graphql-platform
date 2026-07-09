using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Projections;

public class CatchErrorMiddlewareAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member)
    {
        descriptor.Use(
            static next => async ctx =>
            {
                try
                {
                    await next(ctx);
                }
                catch (Exception ex)
                {
                    ctx.ContextData["ex"] = ex.Message;
                }
            });
    }
}
