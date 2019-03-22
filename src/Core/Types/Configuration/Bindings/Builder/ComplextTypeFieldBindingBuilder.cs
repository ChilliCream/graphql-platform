using System;
using System.Reflection;

namespace HotChocolate.Configuration.Bindings
{
    public class ComplextTypeFieldBindingBuilder
        : IComplextTypeFieldBindingBuilder
    {
        private readonly ComplextTypeFieldBindingInfo _bindingInfo =
            new ComplextTypeFieldBindingInfo();

        public IComplextTypeFieldBindingBuilder SetName(NameString name)
        {
            _bindingInfo.Name = name.EnsureNotEmpty(nameof(name));
            return this;
        }

        public IComplextTypeFieldBindingBuilder SetMember(MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            _bindingInfo.Member = member
                ?? throw new ArgumentNullException(nameof(member));
            return this;
        }

        public bool IsComplete()
        {
            return _bindingInfo.IsValid();
        }

        internal ComplextTypeFieldBindingInfo Create()
        {
            return _bindingInfo.Clone();
        }

        IBindingInfo IBindingBuilder.Create()
        {
            return _bindingInfo.Clone();
        }

        public static ComplextTypeFieldBindingBuilder New() =>
            new ComplextTypeFieldBindingBuilder();
    }
}
