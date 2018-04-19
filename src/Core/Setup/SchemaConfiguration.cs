using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;

namespace HotChocolate
{
    internal class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly List<FieldResolver> _resolverRegistry =
            new List<FieldResolver>();
        private readonly List<FieldResolverDescriptor> _resolverDescriptors =
            new List<FieldResolverDescriptor>();

        private Dictionary<Type, string> _typeAliases = new Dictionary<Type, string>();
        private Dictionary<Type, Dictionary<MemberInfo, string>> _fieldAliases =
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

            FluentFieldMapping<TObjectType> fluentFieldMapping = new FluentFieldMapping<TObjectType>();
            foreach (Action<IFluentFieldMapping<TObjectType>> fieldMapping in fieldMappings)
            {
                fieldMapping(fluentFieldMapping);
            }

            if (_fieldAliases.TryGetValue(typeof(TObjectType), out Dictionary<string, string>()))


                return this;
        }

        public ISchemaConfiguration Name<TObjectType>(
            params Action<IFluentFieldMapping<TObjectType>>[] fieldMapping)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration Resolver(
            string typeName, string fieldName,
            FieldResolverDelegate fieldResolver)
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

        public ISchemaConfiguration Resolver<TResolver>()
        {
            return Resolver<TResolver>(typeof(TResolver).Name);
        }

        public ISchemaConfiguration Resolver<TResolver>(string typeName)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration Resolver<TResolver, TObjectType>()
        {
            return Resolver<TResolver, TObjectType>(typeof(TResolver).Name);
        }

        public ISchemaConfiguration Resolver<TResolver, TObjectType>(string typeName)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<FieldResolver> CreateResolvers()
        {
            throw new NotImplementedException();
        }


    }



}