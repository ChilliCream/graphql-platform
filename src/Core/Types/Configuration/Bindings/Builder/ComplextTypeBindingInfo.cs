using System.Collections.Immutable;
using System;
using HotChocolate.Types;
using System.Linq;

namespace HotChocolate.Configuration.Bindings
{
    internal class ComplextTypeBindingInfo
        : IBindingInfo
    {
        public NameString Name { get; set; }

        public Type Type { get; set; }

        public BindingBehavior BindingBehavior { get; set; }

        public ImmutableList<ComplextTypeFieldBindingInfo> Fields
        { get; set; } = ImmutableList<ComplextTypeFieldBindingInfo>.Empty;

        public bool IsValid()
        {
            if (BindingBehavior == BindingBehavior.Explicit
                && Fields.Count == 0)
            {
                return false;
            }

            if (Type == null)
            {
                return false;
            }

            if (Name.HasValue)
            {
                return Fields.All(t => t.IsValid());
            }

            return false;
        }

        public ComplextTypeBindingInfo Clone()
        {
            return (ComplextTypeBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
