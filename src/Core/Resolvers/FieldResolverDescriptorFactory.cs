using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using HotChocolate.Resolvers;

namespace HotChocolate.Resolvers
{
    internal class FieldResolverDescriptorFactory
    {
        private readonly Dictionary<Type, Type> _resolverObjectTypeMapping =
            new Dictionary<Type, Type>();
        private readonly GetObjectTypeName _getObjectTypeName;
        private readonly GetFieldName _getFieldName;

        public FieldResolverDescriptorFactory(
            Dictionary<Type, Type> resolverObjectTypeMapping,
            GetObjectTypeName getObjectTypeName,
            GetFieldName getFieldName)
        {
            if (resolverObjectTypeMapping == null)
            {
                throw new ArgumentNullException(nameof(resolverObjectTypeMapping));
            }

            if (getObjectTypeName == null)
            {
                throw new ArgumentNullException(nameof(getObjectTypeName));
            }

            if (getFieldName == null)
            {
                throw new ArgumentNullException(nameof(getFieldName));
            }

            _resolverObjectTypeMapping = resolverObjectTypeMapping;
            _getObjectTypeName = getObjectTypeName;
            _getFieldName = getFieldName;
        }

        public IEnumerable<FieldResolverDescriptor> Create()
        {
            foreach (KeyValuePair<Type, Type> mapping in
                _resolverObjectTypeMapping)
            {
                foreach (MemberResolverInfo resolverInfo in
                    ReflectionHelper.GetMemberResolverInfos(mapping.Key))
                {
                    if (mapping.Key == mapping.Value)
                    {
                        yield return CreateResolverDescriptor(
                            resolverInfo, mapping.Key, mapping.Value,
                            _getObjectTypeName(mapping.Value));
                    }
                    else
                    {
                        yield return CreateSourceResolverDescriptor(
                            resolverInfo, mapping.Key, mapping.Value,
                            _getObjectTypeName(mapping.Value));
                    }
                }
            }
        }

        private FieldResolverDescriptor CreateResolverDescriptor(
            MemberResolverInfo resolverInfo, Type resolverType,
            Type objectType, string objectTypeName)
        {
            FieldReference field = new FieldReference(
                objectTypeName, _getFieldName(resolverInfo, resolverType));

            if (resolverInfo.Member is PropertyInfo p)
            {
                return FieldResolverDescriptor.CreateCollectionProperty(
                    field, resolverType, objectType, p);
            }
            else if (resolverInfo.Member is MethodInfo m)
            {
                bool isAsync = typeof(Task).IsAssignableFrom(m.ReturnType);
                IReadOnlyCollection<FieldResolverArgumentDescriptor> argumentDescriptors =
                    CreateResolverArgumentDescriptors(m, resolverType, objectType);
                return FieldResolverDescriptor.CreateCollectionMethod(
                    field, resolverType, objectType, m, isAsync, argumentDescriptors);
            }

            throw new NotSupportedException();
        }

        private FieldResolverDescriptor CreateSourceResolverDescriptor(
           MemberResolverInfo resolverInfo, Type resolverType,
           Type objectType, string objectTypeName)
        {
            FieldReference field = new FieldReference(
                objectTypeName, _getFieldName(resolverInfo, resolverType));

            if (resolverInfo.Member is PropertyInfo p)
            {
                return FieldResolverDescriptor.CreateSourceProperty(
                    field, objectType, p);
            }
            else if (resolverInfo.Member is MethodInfo m)
            {
                bool isAsync = typeof(Task).IsAssignableFrom(m.ReturnType);
                IReadOnlyCollection<FieldResolverArgumentDescriptor> argumentDescriptors =
                    CreateResolverArgumentDescriptors(m, resolverType, objectType);
                return FieldResolverDescriptor.CreateSourceMethod(
                    field, objectType, m, isAsync, argumentDescriptors);
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
