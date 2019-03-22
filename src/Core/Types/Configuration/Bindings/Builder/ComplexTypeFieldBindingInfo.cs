using System.Reflection;

namespace HotChocolate.Configuration.Bindings
{
    internal class ComplexTypeFieldBindingInfo
        : IBindingInfo
    {
        public NameString Name { get; set; }

        public MemberInfo Member { get; set; }

        public bool IsValid()
        {
            return Member != null;
        }

        public ComplexTypeFieldBindingInfo Clone()
        {
            return (ComplexTypeFieldBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
