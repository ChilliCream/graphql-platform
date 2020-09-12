using System;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration.Bindings
{
    internal class ResolverBindingInfo
        : IBindingInfo
    {
        public NameString TypeName { get; set; }

        public NameString FieldName { get; set; }

        public Type SourceType { get; set; }

        public MemberInfo Member { get; set; }

        public FieldResolverDelegate Resolver { get; set; }

        public bool IsValid()
        {
            if (Resolver is null)
            {
                return false;
            }

            if (TypeName.HasValue && FieldName.HasValue)
            {
                return true;
            }

            if (SourceType != null && Member != null)
            {
                return true;
            }

            return false;
        }

        public ResolverBindingInfo Clone()
        {
            return (ResolverBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
