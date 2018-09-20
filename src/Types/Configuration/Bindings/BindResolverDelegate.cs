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

        public void To(string typeName, string fieldName)
        {
            if (typeName?.Length == 0)
            {
                throw new ArgumentException(
                    "The type name has to be specified in order " +
                    "to bind a resolver.",
                    nameof(typeName));
            }

            if (fieldName?.Length == 0)
            {
                throw new ArgumentException(
                    "The field name has to be specified in order " +
                    "to bind a resolver.",
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
