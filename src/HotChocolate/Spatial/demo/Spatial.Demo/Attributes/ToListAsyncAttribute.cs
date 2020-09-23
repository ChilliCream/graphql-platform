using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using Microsoft.EntityFrameworkCore;

namespace Spatial.Demo
{
    public class ToListAsyncAttribute : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Use(
                next => async ctx =>
                {
                    await next(ctx);

                    if (ctx.Result is IQueryable<County> result)
                    {
                        ctx.Result = await result.ToListAsync();
                    }
                });
        }
    }
}
