using System;

namespace HotChocolate.Configuration
{
    internal class BindResolver<TResolver>
        : IBindResolver<TResolver>
        where TResolver : class
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;

        public BindResolver(ResolverCollectionBindingInfo bindingInfo)
        {
            _bindingInfo = bindingInfo
                ?? throw new ArgumentNullException(nameof(bindingInfo));
        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            var bindingInfo = new FieldResolverBindungInfo
            {
                FieldName = fieldName.EnsureNotEmpty(nameof(fieldName))
            };
            _bindingInfo.Fields.Add(bindingInfo);

            return new BindFieldResolver<TResolver>(_bindingInfo, bindingInfo);
        }

        public IBoundResolver<TResolver> To(NameString typeName)
        {
            _bindingInfo.ObjectTypeName =
                typeName.EnsureNotEmpty(nameof(typeName));
            return this;
        }

        public IBoundResolver<TResolver, TObjectType> To<TObjectType>()
            where TObjectType : class
        {
            _bindingInfo.ObjectType = typeof(TObjectType);
            return new BoundResolver<TResolver, TObjectType>(_bindingInfo);
        }
    }
}
