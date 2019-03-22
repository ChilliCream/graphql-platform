using System;
using System.Linq.Expressions;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration.Bindings
{
    internal class BindResolverDelegate
        : IBindResolverDelegate
    {
        private readonly IResolverBindingBuilder _bindingBuilder;

        public BindResolverDelegate(IResolverBindingBuilder bindingBuilder)
        {
            _bindingBuilder = bindingBuilder
                ?? throw new ArgumentNullException(nameof(bindingBuilder));
        }

        public void To(NameString typeName, NameString fieldName)
        {
            _bindingBuilder.SetType(typeName.EnsureNotEmpty(nameof(typeName)))
                .SetField(fieldName.EnsureNotEmpty(nameof(fieldName)));
        }

        public void To<TObjectType>(
            Expression<Func<TObjectType, object>> resolver)
            where TObjectType : class
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _bindingBuilder.SetType(typeof(TObjectType))
                .SetField(resolver.ExtractMember());
        }
    }

    internal class BindResolver<TResolver>
        : IBindResolver<TResolver>
        where TResolver : class
    {
        public BindResolver(IResolverTypeBindingBuilder builder)
        {

        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            throw new NotImplementedException();
        }

        public IBoundResolver<TResolver> To(NameString typeName)
        {
            throw new NotImplementedException();
        }

        public IBoundResolver<TResolver, TObjectType> To<TObjectType>() where TObjectType : class
        {
            throw new NotImplementedException();
        }
    }

    internal class BindFieldResolver<TResolver>
        : IBindFieldResolver<TResolver>
        where TResolver : class
    {
        public IBoundResolver<TResolver> With<TPropertyType>(Expression<Func<TResolver, TPropertyType>> resolver)
        {
            throw new NotImplementedException();
        }
    }

    internal class BindFieldResolver<TResolver, TObjectType>
        : IBindFieldResolver<TResolver, TObjectType>
        where TResolver : class
        where TObjectType : class
    {
        public IBoundResolver<TResolver, TObjectType> With<TPropertyType>(Expression<Func<TResolver, TPropertyType>> resolver)
        {
            throw new NotImplementedException();
        }
    }

    internal class BoundResolver<TResolver>
        : IBoundResolver<TResolver>
        where TResolver : class
    {
        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            throw new NotImplementedException();
        }
    }

    internal class BoundResolver<TResolver, TObjectType>
        : IBoundResolver<TResolver, TObjectType>
        where TResolver : class
        where TObjectType : class
    {
        public IBindFieldResolver<TResolver> Resolve<TPropertyType>(Expression<Func<TObjectType, TPropertyType>> field)
        {
            throw new NotImplementedException();
        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            throw new NotImplementedException();
        }
    }

}
