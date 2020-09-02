using System.Collections.Immutable;
using System;
using HotChocolate.Types;

namespace HotChocolate.Configuration.Bindings
{
    internal class ComplexTypeBindingInfo
        : IBindingInfo
    {
        public NameString Name { get; set; }

        public Type Type { get; set; }

        public BindingBehavior BindingBehavior { get; set; }

        public ImmutableList<ComplexTypeFieldBindingInfo> Fields
        { get; set; } = ImmutableList<ComplexTypeFieldBindingInfo>.Empty;

        public bool IsValid()
        {
            if (BindingBehavior == BindingBehavior.Explicit
                && Fields.Count == 0)
            {
                return false;
            }

            if (Type is null)
            {
                return false;
            }

            return true;
        }

        public ComplexTypeBindingInfo Clone()
        {
            return (ComplexTypeBindingInfo)MemberwiseClone();
        }

        IBindingInfo IBindingInfo.Clone() => Clone();
    }
}
