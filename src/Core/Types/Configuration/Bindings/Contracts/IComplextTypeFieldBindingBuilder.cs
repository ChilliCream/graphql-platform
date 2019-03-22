using System.Reflection;

namespace HotChocolate.Configuration.Bindings
{
    public interface IComplextTypeFieldBindingBuilder
        : IBindingBuilder
    {
        IComplextTypeFieldBindingBuilder SetName(NameString name);
        IComplextTypeFieldBindingBuilder SetMember(MemberInfo member);
    }
}
