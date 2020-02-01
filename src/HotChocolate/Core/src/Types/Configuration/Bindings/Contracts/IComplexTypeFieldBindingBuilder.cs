using System.Reflection;

namespace HotChocolate.Configuration.Bindings
{
    public interface IComplexTypeFieldBindingBuilder
        : IBindingBuilder
    {
        IComplexTypeFieldBindingBuilder SetName(NameString name);
        IComplexTypeFieldBindingBuilder SetMember(MemberInfo member);
    }
}
