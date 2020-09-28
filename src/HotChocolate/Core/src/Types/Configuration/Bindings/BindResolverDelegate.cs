using System;
using System.Linq.Expressions;
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
            if (resolver is null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _bindingBuilder.SetType(typeof(TObjectType))
                .SetField(resolver.ExtractMember());
        }
    }
}
