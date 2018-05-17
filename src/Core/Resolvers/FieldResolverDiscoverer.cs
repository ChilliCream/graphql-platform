using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Resolvers
{
    internal class FieldResolverDiscoverer
    {
        private readonly GetObjectTypeName _getObjectTypeName;
        private readonly GetFieldName _getFieldName;

        public FieldResolverDiscoverer(
            GetObjectTypeName getObjectTypeName,
            GetFieldName getFieldName)
        {
            if (getObjectTypeName == null)
            {
                throw new ArgumentNullException(nameof(getObjectTypeName));
            }

            if (getFieldName == null)
            {
                throw new ArgumentNullException(nameof(getFieldName));
            }

            _getObjectTypeName = getObjectTypeName;
            _getFieldName = getFieldName;
        }

        public IEnumerable<FieldResolverDescriptor> GetPossibleResolvers(
            Type resolverType, Type sourceType)
        {
            foreach (MemberResolverInfo resolverInfo in
                ReflectionHelper.GetMemberResolverInfos(resolverType))
            {
                FieldReference field = new FieldReference(
                    _getObjectTypeName(sourceType),
                    _getFieldName(resolverInfo, resolverType));

                if (resolverType == sourceType)
                {
                    yield return CreateSourceResolverDescriptor(
                        resolverInfo, resolverType, sourceType, field);
                }
                else
                {
                    yield return CreateResolverDescriptor(
                        resolverInfo, resolverType, sourceType, field);
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
    }
}
