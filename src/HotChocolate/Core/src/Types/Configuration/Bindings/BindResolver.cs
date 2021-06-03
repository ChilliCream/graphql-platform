using System;

namespace HotChocolate.Configuration.Bindings
{
    internal class BindResolver<TResolver>
        : IBindResolver<TResolver>
        where TResolver : class
    {
        private readonly IResolverTypeBindingBuilder _typeBuilder;

        public BindResolver(IResolverTypeBindingBuilder typeBuilder)
        {
            _typeBuilder = typeBuilder
                ?? throw new ArgumentNullException(nameof(typeBuilder));
        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            IResolverFieldBindingBuilder fieldBuilder =
                ResolverFieldBindingBuilder.New()
                    .SetField(fieldName);
            return new BindFieldResolver<TResolver>(
                _typeBuilder, fieldBuilder);
        }

        public IBoundResolver<TResolver> To(NameString typeName)
        {
            return new BoundResolver<TResolver>(
                _typeBuilder.SetType(typeName));
        }

        public IBoundResolver<TResolver, TObjectType> To<TObjectType>()
            where TObjectType : class
        {
            return new BoundResolver<TResolver, TObjectType>(
                _typeBuilder.SetType(typeof(TObjectType)));
        }
    }
}
