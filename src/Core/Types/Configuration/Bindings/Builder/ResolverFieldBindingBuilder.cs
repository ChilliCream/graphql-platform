using System;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration.Bindings
{
    public class ResolverFieldBindingBuilder
        : IResolverFieldBindingBuilder
    {
        private readonly ResolverFieldBindingInfo _bindingInfo =
            new ResolverFieldBindingInfo();

        public IResolverFieldBindingBuilder SetField(NameString fieldName)
        {
            _bindingInfo.FieldName =
                fieldName.EnsureNotEmpty(nameof(fieldName));
            return this;
        }

        public IResolverFieldBindingBuilder SetField(MemberInfo member)
        {
            _bindingInfo.FieldMember = member
                ?? throw new ArgumentNullException(nameof(member));
            return this;
        }

        public IResolverFieldBindingBuilder SetResolver(MemberInfo member)
        {
            _bindingInfo.ResolverMember = member
                ?? throw new ArgumentNullException(nameof(member));
            return this;
        }

        public IResolverFieldBindingBuilder SetResolver(
            FieldResolverDelegate resolver)
        {
            _bindingInfo.ResolverDelegate = resolver
                ?? throw new ArgumentNullException(nameof(resolver));
            return this;
        }

        public bool IsComplete()
        {
            return _bindingInfo.IsValid();
        }

        internal ResolverFieldBindingInfo Create()
        {
            return _bindingInfo.Clone();
        }

        IBindingInfo IBindingBuilder.Create() => Create();

        public static ResolverFieldBindingBuilder New() =>
            new ResolverFieldBindingBuilder();
    }
}
