using System.Reflection;
using System;
using HotChocolate.Types.Descriptors.Definitions;
using System.Collections.Generic;
using System.Linq;

namespace HotChocolate.Types.Descriptors
{
    public interface IDescriptorContext
    {
        INamingConventions Naming { get; }

        ITypeInspector Inspector { get; }
    }


    public interface INamingConventions
    {
        NameString GetTypeName(Type type);

        string GetTypeDescription(Type type);

        NameString GetMemberName(MemberInfo member);

        string GetMemberDescription(MemberInfo member);

        NameString GetEnumValueName(object value);
    }

    public class DefaultNamingConventions
        : INamingConventions
    {
        public NameString GetEnumValueName(object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.ToString().ToUpperInvariant();
        }

        public string GetMemberDescription(MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public NameString GetMemberName(MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public string GetTypeDescription(Type type)
        {
            throw new NotImplementedException();
        }

        public NameString GetTypeName(Type type)
        {
            throw new NotImplementedException();
        }
    }

    public interface ITypeInspector
    {
        IEnumerable<Type> GetResolverTypes(Type sourceType);
        IEnumerable<MemberInfo> GetMembers(Type type);
        ITypeReference GetReturnType(MemberInfo member, TypeContext context);
    }

    public class DefaultTypeInspector
        : ITypeInspector
    {
        public IEnumerable<MemberInfo> GetMembers(Type type)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Type> GetResolverTypes(Type sourceType)
        {
            if (sourceType.IsDefined(typeof(GraphQLResolverAttribute)))
            {
                return sourceType
                    .GetCustomAttributes(typeof(GraphQLResolverAttribute))
                    .OfType<GraphQLResolverAttribute>()
                    .SelectMany(attr => attr.ResolverTypes);
            }
            return Enumerable.Empty<Type>();
        }

        public virtual ITypeReference GetReturnType(
            MemberInfo member,
            TypeContext context)
        {
            Type returnType = GetReturnType(member);

            if (member.IsDefined(typeof(GraphQLNonNullTypeAttribute)))
            {
                var attribute =
                    member.GetCustomAttribute<GraphQLNonNullTypeAttribute>();

                return new ClrTypeReference(
                    returnType,
                    context,
                    attribute.IsNullable,
                    attribute.IsElementNullable);
            }

            return new ClrTypeReference(returnType, context);
        }

        protected static Type GetReturnType(MemberInfo member)
        {
            if (member is MethodInfo m)
            {
                return m.ReturnType;
            }
            else if (member is PropertyInfo p)
            {
                return p.PropertyType;
            }
            else
            {
                // TODO : resources
                throw new ArgumentException("TODO", nameof(member));
            }
        }


    }

    public static class TypeInspectorExtensions
    {
        public static ITypeReference GetInputReturnType(
            this ITypeInspector typeInspector,
            MemberInfo member)
        {
            return typeInspector.GetReturnType(member, TypeContext.Input);
        }

        public static ITypeReference GetOutputReturnType(
            this ITypeInspector typeInspector,
            MemberInfo member)
        {
            return typeInspector.GetReturnType(member, TypeContext.Output);
        }
    }
}
