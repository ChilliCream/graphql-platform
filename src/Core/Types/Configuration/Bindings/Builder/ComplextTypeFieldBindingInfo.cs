using System.Reflection;

namespace HotChocolate.Configuration.Bindings
{
    internal class ComplextTypeFieldBindingInfo
        : IBindingInfo
    {
        public NameString Name { get; set; }

        public MemberInfo Member { get; set; }

        public bool IsValid()
        {
            return Name.HasValue && Member != null;
        }

        public ComplextTypeFieldBindingInfo Clone()
        {
            return (ComplextTypeFieldBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
