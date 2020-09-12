using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration.Bindings
{
    internal class BindFieldResolver<TResolver>
        : IBindFieldResolver<TResolver>
        where TResolver : class
    {
        private readonly IResolverTypeBindingBuilder _typeBuilder;
        private readonly IResolverFieldBindingBuilder _fieldBuilder;

        public BindFieldResolver(
            IResolverTypeBindingBuilder typeBuilder,
            IResolverFieldBindingBuilder fieldBuilder)
        {
            _typeBuilder = typeBuilder
                ?? throw new ArgumentNullException(nameof(typeBuilder));
            _fieldBuilder = fieldBuilder
                ?? throw new ArgumentNullException(nameof(fieldBuilder));
        }

        public IBoundResolver<TResolver> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _fieldBuilder.SetResolver(resolver.ExtractMember());
            _typeBuilder.AddField(_fieldBuilder);

            return new BoundResolver<TResolver>(_typeBuilder);
        }
    }

    internal class BindFieldResolver<TResolver, TObjectType>
        : IBindFieldResolver<TResolver, TObjectType>
        where TResolver : class
        where TObjectType : class
    {
        private readonly IResolverTypeBindingBuilder _typeBuilder;
        private readonly IResolverFieldBindingBuilder _fieldBuilder;

        public BindFieldResolver(
            IResolverTypeBindingBuilder typeBuilder,
            IResolverFieldBindingBuilder fieldBuilder)
        {
            _typeBuilder = typeBuilder
                ?? throw new ArgumentNullException(nameof(typeBuilder));
            _fieldBuilder = fieldBuilder
                ?? throw new ArgumentNullException(nameof(fieldBuilder));
        }

        public IBoundResolver<TResolver, TObjectType> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver)
        {
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _fieldBuilder.SetResolver(resolver.ExtractMember());
            _typeBuilder.AddField(_fieldBuilder);

            return new BoundResolver<TResolver, TObjectType>(_typeBuilder);
        }
    }
}
