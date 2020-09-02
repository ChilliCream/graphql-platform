using System.Collections.Immutable;
using System;
using HotChocolate.Types;
using System.Linq;

namespace HotChocolate.Configuration.Bindings
{
    internal class ResolverTypeBindingInfo
        : IBindingInfo
    {
        public Type ResolverType { get; set; }

        public Type SourceType { get; set; }

        public NameString TypeName { get; set; }

        public BindingBehavior BindingBehavior { get; set; }

        public ImmutableList<ResolverFieldBindingInfo> Fields { get; set; } =
            ImmutableList<ResolverFieldBindingInfo>.Empty;

        public bool IsValid()
        {
            if (BindingBehavior == BindingBehavior.Explicit
                && Fields.Count == 0)
            {
                return false;
            }

            if (ResolverType is null)
            {
                return false;
            }

            if (SourceType != null || TypeName.HasValue)
            {
                return Fields.All(t => t.IsValid());
            }

            return false;
        }

        public ResolverTypeBindingInfo Clone()
        {
            return (ResolverTypeBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
