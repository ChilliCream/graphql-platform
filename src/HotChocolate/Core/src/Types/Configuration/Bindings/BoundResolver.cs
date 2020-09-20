using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration.Bindings
{
    internal class BoundResolver<TResolver>
        : IBoundResolver<TResolver>
        where TResolver : class
    {
        private readonly IResolverTypeBindingBuilder _builder;

        public BoundResolver(IResolverTypeBindingBuilder builder)
        {
            _builder = builder
                ?? throw new ArgumentNullException(nameof(builder));
        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            IResolverFieldBindingBuilder builder =
                ResolverFieldBindingBuilder.New()
                    .SetField(fieldName);
            return new BindFieldResolver<TResolver>(_builder, builder);
        }
    }

    internal class BoundResolver<TResolver, TObjectType>
        : IBoundResolver<TResolver, TObjectType>
        where TResolver : class
        where TObjectType : class
    {
        private readonly IResolverTypeBindingBuilder _builder;

        public BoundResolver(IResolverTypeBindingBuilder builder)
        {
            _builder = builder
                ?? throw new ArgumentNullException(nameof(builder));
        }

        public IBindFieldResolver<TResolver, TObjectType> Resolve<TPropType>(
            Expression<Func<TObjectType, TPropType>> field)
        {
            if (field is null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            IResolverFieldBindingBuilder builder =
               ResolverFieldBindingBuilder.New()
                   .SetField(field.ExtractMember());
            return new BindFieldResolver<TResolver, TObjectType>(
                _builder, builder);
        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            IResolverFieldBindingBuilder builder =
               ResolverFieldBindingBuilder.New()
                   .SetField(fieldName);
            return new BindFieldResolver<TResolver>(_builder, builder);
        }
    }
}
