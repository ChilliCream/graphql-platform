using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

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
            _bindingInfo = bindingInfo
                ?? throw new ArgumentNullException(nameof(bindingInfo));
            _fieldBindingInfo = fieldBindingInfo
                ?? throw new ArgumentNullException(nameof(fieldBindingInfo));
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
