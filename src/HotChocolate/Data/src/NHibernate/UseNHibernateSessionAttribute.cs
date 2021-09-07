namespace HotChocolate.Data
{
    using System.Reflection;
    using Types;
    using Types.Descriptors;

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
