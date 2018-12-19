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
            _bindingInfo.ObjectTypeName =
                typeName.EnsureNotEmpty(nameof(typeName));
            _bindingInfo.FieldName =
                fieldName.EnsureNotEmpty(nameof(fieldName));
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
