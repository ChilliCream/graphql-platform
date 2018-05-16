using System;
using System.Collections.Generic;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate
{
    internal class SchemaConfiguration2
        : ISchemaConfiguration2
    {
        private readonly List<IFieldResolverBinding> _fieldResolverBindings =
            new List<IFieldResolverBinding>();

        public ISchemaConfiguration2 BindResolver(
            string typeName, string fieldName,
            FieldResolverDelegate fieldResolver)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            _fieldResolverBindings.Add(new DelegateFieldResolverBinding(typeName, fieldName, fieldResolver));
            return this;
        }

        public ISchemaConfiguration2 BindResolver(
            string typeName, string fieldName,
            AsyncFieldResolverDelegate fieldResolver)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (string.IsNullOrEmpty(fieldName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            _fieldResolverBindings.Add(new DelegateFieldResolverBinding(typeName, fieldName, fieldResolver));
            return this;
        }

        public ISchemaConfiguration2 BindResolver<TResolver, TObjectType>()
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 BindResolver<TResolver, TObjectType>(
            params Action<IFluentFieldMapping<TResolver>>[] fieldBindings)
        {
            if(fieldBindings == null || fieldBindings.Length == 0)
            {
                return BindResolverå
            }

            CollectionFieldResolverBinding binding =
                new CollectionFieldResolverBinding(
                    typeof(TObjectType), typeof(TResolver),
                    CreateFieldBindings<TResolver>(fieldBindings));
            _fieldResolverBindings.Add(binding);
            return this;
        }

        public ISchemaConfiguration2 BindType<T>(string typeName)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 BindType<T>(string typeName, params Action<IFluentFieldMapping<T>>[] fieldMapping)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 BindType<T>(params Action<IFluentFieldMapping<T>>[] fieldMapping)
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 RegisterScalarType<T>(T type) where T : ScalarType
        {
            throw new NotImplementedException();
        }

        private string GetTypeName(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute)))
            {
                return type.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }
            return type.Name;
        }

        private Dictionary<string, MemberInfo> CreateFieldBindings<T>(
            Action<IFluentFieldMapping<T>>[] fieldMappings)
        {
            FluentFieldMapping<T> fluentFieldMapping =
                new FluentFieldMapping<T>();
            foreach (Action<IFluentFieldMapping<T>> fieldMapping in
                fieldMappings)
            {
                fieldMapping(fluentFieldMapping);
            }

            Dictionary<string, MemberInfo> mappings =
                new Dictionary<string, MemberInfo>();
            foreach (KeyValuePair<MemberInfo, string> fieldMapping in
                fluentFieldMapping.Mappings)
            {
                mappings[fieldMapping.Value] = fieldMapping.Key;
            }
            return mappings;
        }


    }


}
