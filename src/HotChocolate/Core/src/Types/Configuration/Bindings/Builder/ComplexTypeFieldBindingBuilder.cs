using System;
using System.Reflection;

namespace HotChocolate.Configuration.Bindings
{
    public class ComplexTypeFieldBindingBuilder
        : IComplexTypeFieldBindingBuilder
    {
        private readonly ComplexTypeFieldBindingInfo _bindingInfo =
            new ComplexTypeFieldBindingInfo();

        public IComplexTypeFieldBindingBuilder SetName(NameString name)
        {
            _bindingInfo.Name = name.EnsureNotEmpty(nameof(name));
            return this;
        }

        public IComplexTypeFieldBindingBuilder SetMember(MemberInfo member)
        {
            _bindingInfo.Member = member ?? 
                throw new ArgumentNullException(nameof(member));
            return this;
        }

        public bool IsComplete()
        {
            return _bindingInfo.IsValid();
        }

        internal ComplexTypeFieldBindingInfo Create()
        {
            return _bindingInfo.Clone();
        }

        IBindingInfo IBindingBuilder.Create()
        {
            return _bindingInfo.Clone();
        }

        public static ComplexTypeFieldBindingBuilder New() =>
            new ComplexTypeFieldBindingBuilder();
    }
}
