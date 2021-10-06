using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data
{
    public class UseNHibernateSessionAttribute : ObjectFieldDescriptorAttribute
    {
        private static readonly MethodInfo _nHibernateContext =
            typeof(NHibernateObjectFieldDescriptorExtensions)
                .GetMethod(
                    nameof(NHibernateObjectFieldDescriptorExtensions.UseNHibernateSession),
                    BindingFlags.Public | BindingFlags.Static)!;


        public override void OnConfigure(IDescriptorContext context, IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            _nHibernateContext.Invoke(null, new object?[] {descriptor});
        }
    }
}
