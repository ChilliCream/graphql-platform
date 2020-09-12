using System;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate.Configuration.Bindings
{
    public class ResolverBindingBuilder
        : IResolverBindingBuilder
    {
        private readonly ResolverBindingInfo _bindingInfo =
            new ResolverBindingInfo();

        public IResolverBindingBuilder SetResolver(
            FieldResolverDelegate resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _bindingInfo.Resolver = resolver;
            return this;
        }

        public IResolverBindingBuilder SetType(NameString typeName)
        {
            _bindingInfo.TypeName = typeName;
            return this;
        }

        public IResolverBindingBuilder SetType(Type type)
        {
            _bindingInfo.SourceType = type;
            return this;
        }

        public IResolverBindingBuilder SetField(NameString fieldName)
        {
            _bindingInfo.FieldName = fieldName;
            return this;
        }

        public IResolverBindingBuilder SetField(MemberInfo member)
        {
            _bindingInfo.Member = member;
            return this;
        }

        public bool IsComplete()
        {
            return _bindingInfo.IsValid();
        }

        public IBindingInfo Create()
        {
            return _bindingInfo.Clone();
        }

        public static ResolverBindingBuilder New() =>
            new ResolverBindingBuilder();
    }
}
