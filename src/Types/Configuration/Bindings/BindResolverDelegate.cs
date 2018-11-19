using System;
using System.Linq.Expressions;
using HotChocolate.Utilities;

namespace HotChocolate.Configuration
{
    internal class BindResolverDelegate
        : IBindResolverDelegate
    {
        private readonly ResolverDelegateBindingInfo _bindingInfo;

        public BindResolverDelegate(ResolverDelegateBindingInfo bindingInfo)
        {
            _bindingInfo = bindingInfo
                ?? throw new ArgumentNullException(nameof(bindingInfo));
        }

        public void To(NameString typeName, NameString fieldName)
        {
            if (typeName.IsEmpty)
            {
                throw new ArgumentException(
                    TypeResources.Name_Cannot_BeEmpty(),
                    nameof(typeName));
            }

            if (fieldName.IsEmpty)
            {
                throw new ArgumentException(
                    TypeResources.Name_Cannot_BeEmpty(),
                    nameof(fieldName));
            }

            _bindingInfo.ObjectTypeName = typeName;
            _bindingInfo.FieldName = fieldName;
        }

        public void To<TObjectType>(
            Expression<Func<TObjectType, object>> resolver)
            where TObjectType : class
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            _bindingInfo.ObjectType = typeof(TObjectType);
            _bindingInfo.FieldMember = resolver.ExtractMember();
        }
    }
}
