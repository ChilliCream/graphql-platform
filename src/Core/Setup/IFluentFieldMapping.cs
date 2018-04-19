using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate
{
    public interface IFluentFieldMapping<TObjectType>
    {
        IFluentFieldMapping<TObjectType> Field<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field,
            string fieldName);
    }

    internal sealed class FluentFieldMapping<TObjectType>
        : IFluentFieldMapping<TObjectType>
    {
        private readonly Dictionary<MemberInfo, string> _mappings = new Dictionary<MemberInfo, string>();

        public IReadOnlyDictionary<MemberInfo, string> Mappings => _mappings;

        public IFluentFieldMapping<TObjectType> Field<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> field,
            string fieldName)
        {
            _mappings[GetFieldReference(field)] = fieldName;
            return this;
        }

        private static MemberInfo GetFieldReference<TPropertyType>(
            Expression<Func<TObjectType, TPropertyType>> propertyExpression)
        {
            Type type = typeof(TObjectType);

            if (propertyExpression.Body is MemberExpression m)
            {
                if (m.Member is PropertyInfo pi
                    && type == pi.ReflectedType)
                {
                    return pi;
                }
                else if (m.Member is MethodInfo mi
                    && type == mi.ReflectedType)
                {
                    return mi;
                }
            }

            throw new ArgumentException(
                "The specied expression does not refer to a property.",
                nameof(propertyExpression));
        }
    }
}