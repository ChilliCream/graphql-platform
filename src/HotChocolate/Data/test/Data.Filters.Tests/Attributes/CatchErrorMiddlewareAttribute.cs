using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Filters;

public class CatchErrorMiddlewareAttribute : ObjectFieldDescriptorAttribute
{
    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo member)
    {
        descriptor.Use(n => async c =>
        {
            try
            {
                await n(c);
            }
            catch (Exception ex)
            {
                c.ContextData["ex"] = ex.Message;
            }
        });
    }
}
