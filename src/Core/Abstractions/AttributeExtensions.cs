﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Language;

namespace HotChocolate
{
    internal static class AttributeExtensions
    {
        private const string _get = "Get";
        private const string _async = "Async";
        private const string _typePostfix = "`1";

        public static string GetGraphQLName(this Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            TypeInfo typeInfo = type.GetTypeInfo();
            string name = typeInfo.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? typeInfo.GetCustomAttribute<GraphQLNameAttribute>().Name
                : GetFromType(typeInfo);
            return NameUtils.MakeValidGraphQLName(name);
        }

        public static string GetGraphQLName(this PropertyInfo property)
        {
            string name = property.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? property.GetCustomAttribute<GraphQLNameAttribute>().Name
                : NormalizeName(property.Name);
            return NameUtils.MakeValidGraphQLName(name);
        }

        public static string GetGraphQLName(this MethodInfo method)
        {
            string name = method.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? method.GetCustomAttribute<GraphQLNameAttribute>().Name
                : NormalizeMethodName(method);
            return NameUtils.MakeValidGraphQLName(name);
        }

        public static string GetGraphQLName(this ParameterInfo parameter)
        {
            string name = parameter.IsDefined(
                typeof(GraphQLNameAttribute), false)
                ? parameter.GetCustomAttribute<GraphQLNameAttribute>().Name
                : NormalizeName(parameter.Name);
            return NameUtils.MakeValidGraphQLName(name);
        }

        public static string GetGraphQLName(this MemberInfo member)
        {
            if (member == null)
            {
                throw new ArgumentNullException(nameof(member));
            }

            if (member is MethodInfo m)
            {
                return GetGraphQLName(m);
            }

            if (member is PropertyInfo p)
            {
                return GetGraphQLName(p);
            }

            throw new NotSupportedException(
                "Only properties and methods are accepted as members.");
        }

        private static string NormalizeMethodName(MethodInfo method)
        {
            string name = method.Name;

            if (name.StartsWith(_get, StringComparison.Ordinal)
                && name.Length > _get.Length)
            {
                name = name.Substring(_get.Length);
            }

            if (typeof(Task).IsAssignableFrom(method.ReturnType)
                && name.Length > _async.Length
                && name.EndsWith(_async, StringComparison.Ordinal))
            {
                name = name.Substring(0, name.Length - _async.Length);
            }

            return NormalizeName(name);
        }

        public static string GetGraphQLDescription(
            this ICustomAttributeProvider attributeProvider)
        {
            if (attributeProvider.IsDefined(
                typeof(GraphQLDescriptionAttribute),
                false))
            {
                GraphQLDescriptionAttribute attribute =
                    (GraphQLDescriptionAttribute)
                        attributeProvider.GetCustomAttributes(
                            typeof(GraphQLDescriptionAttribute),
                            false)[0];
                return attribute.Description;
            }

            return null;
        }

        public static bool IsDeprecated(
            this ICustomAttributeProvider attributeProvider,
            out string reason)
        {
            var deprecatedAttribute =
                GetAttributeIfDefined<GraphQLDeprecatedAttribute>(
                    attributeProvider);

            if (deprecatedAttribute != null)
            {
                reason = deprecatedAttribute.DeprecationReason;
                return true;
            }

            var obsoleteAttribute = GetAttributeIfDefined<ObsoleteAttribute>(
                attributeProvider);

            if (obsoleteAttribute != null)
            {
                reason = obsoleteAttribute.Message;
                return true;
            }

            reason = null;
            return false;
        }

        private static string GetFromType(Type type)
        {
            if (type.GetTypeInfo().IsGenericType)
            {
                string name = type.GetTypeInfo()
                    .GetGenericTypeDefinition()
                    .Name;

                name = name.Substring(0, name.Length - _typePostfix.Length);

                IEnumerable<string> arguments = type
                    .GetTypeInfo().GenericTypeArguments
                    .Select(GetFromType);

                return $"{name}Of{string.Join("And", arguments)}";
            }
            return type.Name;
        }

        private static string NormalizeName(string name)
        {
            if (name.Length > 1)
            {
                return name.Substring(0, 1).ToLowerInvariant() +
                    name.Substring(1);
            }
            return name.ToLowerInvariant();
        }

        private static TAttribute GetAttributeIfDefined<TAttribute>(
            ICustomAttributeProvider attributeProvider)
            where TAttribute : Attribute
        {
            Type attributeType = typeof(TAttribute);

            if (attributeProvider.IsDefined(attributeType, false))
            {
                return (TAttribute)attributeProvider
                    .GetCustomAttributes(attributeType, false)[0];
            }

            return null;
        }
    }
}
