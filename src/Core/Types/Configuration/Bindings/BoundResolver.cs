using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration
{
    internal class BoundResolver<TResolver>
       : IBoundResolver<TResolver>
       where TResolver : class
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;

        internal BoundResolver(ResolverCollectionBindingInfo bindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            _bindingInfo = bindingInfo;
        }

        public IBindFieldResolver<TResolver> Resolve(NameString fieldName)
        {
            FieldResolverBindungInfo fieldBindingInfo =
                new FieldResolverBindungInfo
                {
                    FieldName = fieldName
                };
            _bindingInfo.Fields.Add(fieldBindingInfo);
            return new BindFieldResolver<TResolver>(
                _bindingInfo, fieldBindingInfo);
        }
    }

    internal class BoundResolver<TResolver, TObjectType>
        : BoundResolver<TResolver>
        , IBoundResolver<TResolver, TObjectType>
        where TResolver : class
        where TObjectType : class
    {
        private readonly ResolverCollectionBindingInfo _bindingInfo;

        internal BoundResolver(ResolverCollectionBindingInfo bindingInfo)
            : base(bindingInfo)
        {
            if (bindingInfo == null)
            {
                throw new ArgumentNullException(nameof(bindingInfo));
            }

            _bindingInfo = bindingInfo;
        }

        public IBindFieldResolver<TResolver> Resolve<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field)
        {
            if (field == null)
            {
                throw new ArgumentNullException(nameof(field));
            }

            FieldResolverBindungInfo fieldBindingInfo =
                new FieldResolverBindungInfo
                {
                    FieldMember = field.ExtractMember()
                };
            _bindingInfo.Fields.Add(fieldBindingInfo);
            return new BindFieldResolver<TResolver>(
                _bindingInfo, fieldBindingInfo);
        }
    }
}
