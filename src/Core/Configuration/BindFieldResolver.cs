using System;
using System.Linq.Expressions;

namespace HotChocolate.Configuration
{
    internal class BindFieldResolver<TResolver>
        : IBindFieldResolver<TResolver>
        where TResolver : class
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;
        private readonly FieldResolverBindungInfo _fieldBindingInfo;

        internal BindFieldResolver(
            ResolverCollectionBindingInfo bindingInfo,
            FieldResolverBindungInfo fieldBindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            if (fieldBindingInfo == null)
            {
                throw new ArgumentNullException(nameof(fieldBindingInfo));
            }

            _bindingInfo = bindingInfo;
            _fieldBindingInfo = fieldBindingInfo;
        }

        public IBoundResolver<TResolver> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _fieldBindingInfo.ResolverMember = resolver.ExtractMember();
            return new BindResolver<TResolver>(_bindingInfo);
        }
    }
}
