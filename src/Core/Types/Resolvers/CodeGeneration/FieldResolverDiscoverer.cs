using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal static class FieldResolverDiscoverer
    {
        public static IEnumerable<IFieldResolverDescriptor> DiscoverResolvers(
            Type resolverType, Type sourceType, NameString typeName,
            Func<FieldMember, string> lookupFieldName)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (lookupFieldName == null)
            {
                throw new ArgumentNullException(nameof(lookupFieldName));
            }

            return DiscoverResolversInternal(
                resolverType,
                sourceType,
                typeName.EnsureNotEmpty(nameof(typeName)),
                lookupFieldName);
        }

        private static IEnumerable<IFieldResolverDescriptor> DiscoverResolversInternal(
            Type resolverType, Type sourceType, NameString typeName,
            Func<FieldMember, string> lookupFieldName)
        {
            foreach (FieldMember fieldMember in
                DiscoverResolvableMembers(resolverType, typeName)
                    .Select(t => t.WithFieldName(lookupFieldName(t))))
            {
                if (resolverType == null || resolverType == sourceType)
                {
                    yield return CreateMemberResolverDescriptor(
                        sourceType, fieldMember);
                }
                else
                {
                    yield return CreateResolverDescriptor(
                        resolverType, sourceType, fieldMember);
                }
            }
        }

        public static IEnumerable<IFieldResolverDescriptor> CreateResolverDescriptors(
            Type resolverType, Type sourceType,
            IEnumerable<FieldMember> fieldMembers)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (fieldMembers == null)
            {
                throw new ArgumentNullException(nameof(fieldMembers));
            }

            foreach (FieldMember fieldResolverMember in fieldMembers)
            {
                if (resolverType == null || resolverType == sourceType)
                {
                    yield return CreateMemberResolverDescriptor(
                        sourceType, fieldResolverMember);
                }
                else
                {
                    yield return CreateResolverDescriptor(
                        resolverType, sourceType, fieldResolverMember);
                }
            }
        }

        private static ResolverDescriptor CreateResolverDescriptor(
            Type resolverType, Type sourceType, FieldMember fieldMember)
        {
            ArgumentDescriptor[] arguments =
                (fieldMember.Member is MethodInfo m)
                    ? DiscoverArguments(m, sourceType)
                    : Array.Empty<ArgumentDescriptor>();

            return new ResolverDescriptor(
                    resolverType,
                    sourceType,
                    fieldMember,
                    arguments);

            throw new NotSupportedException();
        }

        private static SourceResolverDescriptor CreateMemberResolverDescriptor(
           Type sourceType, FieldMember fieldMember)
        {
            ArgumentDescriptor[] arguments =
                (fieldMember.Member is MethodInfo m)
                    ? DiscoverArguments(m, sourceType)
                    : Array.Empty<ArgumentDescriptor>();

            return new SourceResolverDescriptor(
                sourceType,
                fieldMember,
                arguments);

            throw new NotSupportedException();
        }

        internal static ArgumentDescriptor[] DiscoverArguments(
            MethodInfo method, Type sourceType = null)
        {
            if (method == null)
            {
                throw new ArgumentNullException(nameof(method));
            }

            ParameterInfo[] parameters = method.GetParameters();
            var arguments = new ArgumentDescriptor[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                arguments[i] = new ArgumentDescriptor(
                    parameter.Name,
                    $"v{i}_{parameter.Name}",
                    ArgumentHelper.LookupKind(parameter, sourceType),
                    parameter.ParameterType);
            }

            return arguments;
        }

        public static IEnumerable<FieldMember> DiscoverResolvableMembers(
            Type resolverType, NameString typeName)
        {
            if (resolverType == null)
            {
                throw new ArgumentNullException(nameof(resolverType));
            }

            typeName.EnsureNotEmpty(nameof(typeName));

            return GetProperties(resolverType, typeName)
                .Concat(GetMethods(resolverType, typeName));
        }

        private static IEnumerable<FieldMember> GetProperties(
            Type resolverType, NameString typeName)
        {
            PropertyInfo[] properties = resolverType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute name = property
                        .GetCustomAttribute<GraphQLNameAttribute>();

                    yield return new FieldMember(
                        typeName, name.Name, property);
                }
                else
                {
                    yield return new FieldMember(
                        typeName, AdjustCasing(property.Name), property);
                }
            }
        }

        private static IEnumerable<FieldMember> GetMethods(
            Type resolverType, NameString typeName)
        {
            MethodInfo[] methods = resolverType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute name = method
                        .GetCustomAttribute<GraphQLNameAttribute>();

                    yield return new FieldMember(
                        typeName, name.Name, method);
                }
                else
                {
                    if (method.Name.StartsWith("Get", StringComparison.Ordinal)
                        && method.Name.Length > 3)
                    {
                        yield return new FieldMember(
                            typeName,
                            AdjustCasing(method.Name.Substring(3)),
                            method);
                    }

                    yield return new FieldMember(
                           typeName,
                           AdjustCasing(method.Name),
                           method);
                }
            }
        }

        public static string AdjustCasing(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }
    }
}
