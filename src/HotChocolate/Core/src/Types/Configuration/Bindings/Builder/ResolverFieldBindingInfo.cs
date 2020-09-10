using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration.Bindings
{
    internal class ResolverFieldBindingInfo
        : IBindingInfo
    {
        public NameString FieldName { get; set; }

        public MemberInfo FieldMember { get; set; }

        public MemberInfo ResolverMember { get; set; }

        public FieldResolverDelegate ResolverDelegate { get; set; }

        public bool IsValid()
        {
            if (ResolverMember is null && ResolverDelegate is null)
            {
                return false;
            }

            if (FieldName.HasValue)
            {
                return true;
            }

            if (FieldMember != null)
            {
                return true;
            }

            return false;
        }

        public ResolverFieldBindingInfo Clone()
        {
            return (ResolverFieldBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
