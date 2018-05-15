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
        private readonly List<FieldResolverBinding> _fieldResolverBindings =
            new List<FieldResolverBinding>();

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

        public ISchemaConfiguration2 BindResolver<TResolver, TObjectType>(params Action<IFluentFieldMapping<TResolver>>[] fieldMapping)
        {
            CollectionFieldResolverBinding binding = new CollectionFieldResolverBinding()
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

    internal interface IFieldResolverBinding
    {

    }

    internal class DelegateFieldResolverBinding
        : IFieldResolverBinding
    {
        public DelegateFieldResolverBinding(
            string typeName, string fieldName,
            FieldResolverDelegate resolver)
        {
            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            FieldName = fieldName;
            Resolver = resolver;
        }

        public DelegateFieldResolverBinding(
            string typeName, string fieldName,
            AsyncFieldResolverDelegate fieldResolver)
        {
            if (fieldResolver == null)
            {
                throw new ArgumentNullException(nameof(fieldResolver));
            }

            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            FieldName = fieldName;
            Resolver = new FieldResolverDelegate(
                (ctx, ct) => fieldResolver(ctx, ct));
        }

        public string TypeName { get; }
        public string FieldName { get; }
        public FieldResolverDelegate Resolver { get; }
    }



    internal class CollectionFieldResolverBinding
        : FieldResolverBinding
    {
        public CollectionFieldResolverBinding(
            string typeName,
            Type resolverCollection)
            : base(typeName)
        {
            if (resolverCollection == null)
            {
                throw new ArgumentNullException(nameof(resolverCollection));
            }

            ResolverCollection = resolverCollection;
            ExplicitBindings = new Dictionary<string, MemberInfo>();
        }

        public CollectionFieldResolverBinding(
            string typeName,
            Type resolverCollection,
            Dictionary<string, MemberInfo> explicitBindings)
            : base(typeName)
        {
            if (resolverCollection == null)
            {
                throw new ArgumentNullException(nameof(resolverCollection));
            }

            if (explicitBindings == null)
            {
                throw new ArgumentNullException(nameof(explicitBindings));
            }

            ResolverCollection = resolverCollection;
            ExplicitBindings = explicitBindings;
        }

        // node: a type providing one ore more resolver function hence a resolver collection.
        public Type ResolverCollection { get; }
        public Dictionary<string, MemberInfo> ExplicitBindings { get; }
    }


}
