using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    internal delegate string GetObjectTypeName(Type objectType);

    internal delegate string GetFieldName(
        MemberResolverInfo resolverInfo, Type resolverType);

    internal class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly Dictionary<string, INamedType> _registeredTypes =
            new Dictionary<string, INamedType>();

        private readonly List<FieldResolver> _resolverRegistry =
            new List<FieldResolver>();
        private readonly Dictionary<Type, Type> _resolverObjectTypeMapping =
            new Dictionary<Type, Type>();

        private readonly Dictionary<Type, string> _typeAliases =
            new Dictionary<Type, string>();
        private readonly Dictionary<Type, Dictionary<MemberInfo, string>> _fieldAliases =
            new Dictionary<Type, Dictionary<MemberInfo, string>>();

        public ISchemaConfiguration Name<TObjectType>(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            _typeAliases[typeof(TObjectType)] = typeName;

            return this;
        }

        public ISchemaConfiguration Name<TObjectType>(string typeName,
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMappings)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            _typeAliases[typeof(TObjectType)] = typeName;

            Name<TObjectType>(fieldMappings);

            return this;
        }

        public ISchemaConfiguration Name<TObjectType>(
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMappings)
        {
            Type objectType = typeof(TObjectType);
            if (!_typeAliases.ContainsKey(objectType))
            {
                if (objectType.IsDefined(typeof(GraphQLNameAttribute)))
                {
                    _typeAliases[objectType] = objectType
                        .GetCustomAttribute<GraphQLNameAttribute>().Name;
                }
                else
                {
                    _typeAliases[objectType] = objectType.Name;
                }
            }

            FluentFieldMapping<TObjectType> fluentFieldMapping =
                new FluentFieldMapping<TObjectType>();
            foreach (Action<IFluentFieldMapping<TObjectType>> fieldMapping in
                fieldMappings)
            {
                fieldMapping(fluentFieldMapping);
            }

            if (!_fieldAliases.TryGetValue(typeof(TObjectType),
                out Dictionary<MemberInfo, string> mappings))
            {
                mappings = new Dictionary<MemberInfo, string>();
                _fieldAliases[typeof(TObjectType)] = mappings;
            }

            foreach (KeyValuePair<MemberInfo, string> mapping in
                fluentFieldMapping.Mappings)
            {
                mappings[mapping.Key] = mapping.Value;
            }

            return this;
        }

        public ISchemaConfiguration Register<T>(T type)
            where T : INamedType
        {
            _registeredTypes[type.Name] = type;
            return this;
        }

        public ISchemaConfiguration Resolver(
            string typeName, string fieldName,
            AsyncFieldResolverDelegate fieldResolver)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldName == null)
            {
                throw new ArgumentNullException(nameof(fieldName));
            }

            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            _resolverRegistry.Add(new FieldResolver(
                typeName, fieldName, fieldResolver));
            return this;
        }

        public ISchemaConfiguration Resolver<TResolver, TObjectType>()
        {
            _resolverObjectTypeMapping[typeof(TResolver)] = typeof(TObjectType);
            return this;
        }

        public void Commit(SchemaContext context)
        {
            foreach (INamedType type in _registeredTypes.Values)
            {
                context.RegisterType(type);
            }

            FieldResolverDescriptorFactory fieldResolverDescriptorFactory =
                new FieldResolverDescriptorFactory(
                    _resolverObjectTypeMapping,
                    GetObjectTypeName, GetFieldName);
            FieldResolverBuilder fieldResolverBuilder = new FieldResolverBuilder();
            IEnumerable<FieldResolver> fieldResolvers = fieldResolverBuilder
                .Build(GetBestMatchingFieldResolvers(
                    context, fieldResolverDescriptorFactory.Create()));
            context.RegisterResolvers(fieldResolvers);
            context.RegisterResolvers(_resolverRegistry);
            context.RegisterTypeMappings(_typeAliases.Select(
                t => new KeyValuePair<string, Type>(t.Value, t.Key)));
        }

        private IEnumerable<FieldResolverDescriptor> GetBestMatchingFieldResolvers(
            SchemaContext context,
            IEnumerable<FieldResolverDescriptor> resolverDescriptors)
        {
            foreach (var resolverGroup in resolverDescriptors.GroupBy(r => r.Field))
            {
                FieldReference fieldReference = resolverGroup.Key;
                if (context.TryGetOutputType<ObjectType>(
                        fieldReference.TypeName, out ObjectType type)
                    && type.Fields.TryGetValue(
                        fieldReference.FieldName, out Field field))
                {
                    foreach (FieldResolverDescriptor resolverDescriptor in
                        resolverGroup.OrderByDescending(t => t.ArgumentCount()))
                    {
                        if (AllArgumentsMatch(field, resolverDescriptor))
                        {
                            yield return resolverDescriptor;
                            break;
                        }
                    }
                }
            }
        }

        private bool AllArgumentsMatch(Field field, FieldResolverDescriptor resolverDescriptor)
        {
            foreach (FieldResolverArgumentDescriptor argumentDescriptor in
                resolverDescriptor.Arguments())
            {
                if (!field.Arguments.ContainsKey(argumentDescriptor.Name))
                {
                    return false;
                }

                // TODO : Check that argument types are compatible.
                /*
                if (field.Arguments.TryGetValue(
                    argumentDescriptor.Name, out InputField inputField))
                {

                }
                 */
            }
            return true;
        }

        private string GetObjectTypeName(Type objectType)
        {
            if (_typeAliases.TryGetValue(objectType, out string name))
            {
                return name;
            }
            return objectType.Name;
        }

        private string GetFieldName(MemberResolverInfo resolverInfo, Type resolverType)
        {
            string name = string.IsNullOrEmpty(resolverInfo.Alias)
                ? ReflectionHelper.AdjustCasing(resolverInfo.Member.Name)
                : resolverInfo.Alias;

            if (_fieldAliases.TryGetValue(resolverType,
                out Dictionary<MemberInfo, string> mappings)
                && mappings.TryGetValue(resolverInfo.Member, out string alias))
            {
                name = alias;
            }

            return name;
        }


    }
}
