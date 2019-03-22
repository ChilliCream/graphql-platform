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

    internal class BindFieldResolver<TResolver, TObjectType>
        : IBindFieldResolver<TResolver, TObjectType>
        where TResolver : class
        where TObjectType : class
    {


        public IBoundResolver<TResolver, TObjectType> With<TPropertyType>(
            Expression<Func<TResolver, TPropertyType>> resolver)
        {
            throw new NotImplementedException();
        }
    }


}
