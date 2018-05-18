using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Resolvers
{
    internal class FieldResolverDiscoverer
    {
        public IEnumerable<FieldResolverDescriptor> GetPossibleResolvers(
            Type resolverType, Type sourceType, string typeName,
            Func<FieldResolverMember, string> lookupFieldName)
        {
            foreach (FieldResolverMember fieldResolverMember in
                GetPossibleResolverMembers(resolverType, typeName)
                    .Select(t => t.WithFieldName(lookupFieldName(t))))
            {
                if (resolverType == sourceType)
                {
                    yield return CreateSourceResolverDescriptor(
                        fieldResolverMember, resolverType, sourceType);
                }
                else
                {
                    yield return CreateResolverDescriptor(
                        fieldResolverMember, resolverType, sourceType);
                }
            }
        }

        public IEnumerable<FieldResolverDescriptor> GetSelectedResolvers(
            Type resolverType, Type sourceType,
            IEnumerable<FieldResolverMember> selectedResolvers)
        {
            foreach (FieldResolverMember fieldResolverMember in selectedResolvers)
            {
                if (resolverType == sourceType)
                {
                    yield return CreateSourceResolverDescriptor(
                        fieldResolverMember, resolverType, sourceType);
                }
                else
                {
                    yield return CreateResolverDescriptor(
                        fieldResolverMember, resolverType, sourceType);
                }
            }
        }

        private FieldResolverDescriptor CreateResolverDescriptor(
            FieldResolverMember fieldResolverMember,
            Type resolverType, Type objectType)
        {
            if (fieldResolverMember.Member is PropertyInfo p)
            {
                return FieldResolverDescriptor.CreateCollectionProperty(
                    fieldResolverMember, resolverType, objectType, p);
            }
            else if (fieldResolverMember.Member is MethodInfo m)
            {
                bool isAsync = typeof(Task).IsAssignableFrom(m.ReturnType);
                IReadOnlyCollection<FieldResolverArgumentDescriptor> argumentDescriptors =
                    CreateResolverArgumentDescriptors(m, resolverType, objectType);
                return FieldResolverDescriptor.CreateCollectionMethod(
                    fieldResolverMember, resolverType, objectType, m,
                    isAsync, argumentDescriptors);
            }

            throw new NotSupportedException();
        }

        private FieldResolverDescriptor CreateSourceResolverDescriptor(
           FieldResolverMember fieldResolverMember,
           Type resolverType, Type objectType)
        {
            if (fieldResolverMember.Member is PropertyInfo p)
            {
                return FieldResolverDescriptor.CreateSourceProperty(
                    fieldResolverMember, objectType, p);
            }
            else if (fieldResolverMember.Member is MethodInfo m)
            {
                bool isAsync = typeof(Task).IsAssignableFrom(m.ReturnType);
                IReadOnlyCollection<FieldResolverArgumentDescriptor> argumentDescriptors =
                    CreateResolverArgumentDescriptors(m, resolverType, objectType);
                return FieldResolverDescriptor.CreateSourceMethod(
                    fieldResolverMember, objectType, m, isAsync,
                    argumentDescriptors);
            }

            throw new NotSupportedException();
        }

        private IReadOnlyCollection<FieldResolverArgumentDescriptor> CreateResolverArgumentDescriptors(
            MethodInfo method, Type resolverType, Type objectType)
        {
            List<FieldResolverArgumentDescriptor> arguments =
                new List<FieldResolverArgumentDescriptor>();

            foreach (ParameterInfo parameter in method.GetParameters())
            {
                FieldResolverArgumentKind kind = FieldResolverArgumentDescriptor
                    .LookupKind(parameter.ParameterType, objectType);
                arguments.Add(FieldResolverArgumentDescriptor.Create(
                    parameter.Name, kind, parameter.ParameterType));
            }

            return arguments;
        }

        public static IEnumerable<FieldResolverMember> GetPossibleResolverMembers(
            Type resolverType, string typeName)
        {
            return GetProperties(resolverType, typeName)
                .Concat(GetMethods(resolverType, typeName));
        }

        private static IEnumerable<FieldResolverMember> GetProperties(
            Type resolverType, string typeName)
        {
            PropertyInfo[] properties = resolverType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
            {
                if (property.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute name = property
                        .GetCustomAttribute<GraphQLNameAttribute>();

                    yield return new FieldResolverMember(
                        typeName, name.Name, property);
                }
                else
                {
                    yield return new FieldResolverMember(
                        typeName, AdjustCasing(property.Name), property);
                }
            }
        }

        private static IEnumerable<FieldResolverMember> GetMethods(
            Type resolverType, string typeName)
        {
            MethodInfo[] methods = resolverType.GetMethods(
                BindingFlags.Public | BindingFlags.Instance);

            foreach (MethodInfo method in methods)
            {
                if (method.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    GraphQLNameAttribute name = method
                        .GetCustomAttribute<GraphQLNameAttribute>();

                    yield return new FieldResolverMember(
                        typeName, name.Name, method);
                }
                else
                {
                    if (method.Name.StartsWith("Get") && method.Name.Length > 3)
                    {
                        yield return new FieldResolverMember(
                            typeName,
                            AdjustCasing(method.Name.Substring(3)),
                            method);
                    }

                    yield return new FieldResolverMember(
                           typeName,
                           AdjustCasing(method.Name),
                           method);
                }
            }
        }

        public static string AdjustCasing(string name)
        {
            return name.Substring(0, 1).ToLowerInvariant() + name.Substring(1);
        }
    }
}
