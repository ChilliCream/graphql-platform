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

        public ISchemaConfiguration2 BindResolver<TResolver>()
        {
            Type type = typeof(TResolver);
            string typeName = GetTypeName(type);
            _fieldResolverBindings.Add(
                new CollectionFieldResolverBinding(typeName, type));
            return this;
        }

        public ISchemaConfiguration2 BindResolver<TResolver>(
            params Action<IFluentFieldMapping<TResolver>>[] fieldMapping)
        {
            if (fieldMapping.Length == 0)
            {
                return BindResolver<TResolver>();
            }

            Type type = typeof(TResolver);
            string typeName = GetTypeName(type);
            Dictionary<string, MemberInfo> explicitBindings =
                CreateFieldBindings(fieldMapping);
            _fieldResolverBindings.Add(
                new CollectionFieldResolverBinding(typeName, type));
            return this;
        }

        public ISchemaConfiguration2 BindResolver<TResolver>(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }
            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 BindResolver<TResolver>(
            string name,
            params Action<IFluentFieldMapping<TResolver>>[] fieldMapping)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 BindResolver<TResolver, TObjectType>()
        {
            throw new NotImplementedException();
        }

        public ISchemaConfiguration2 BindResolver<TResolver, TObjectType>(params Action<IFluentFieldMapping<TResolver>>[] fieldMapping)
        {
            throw new NotImplementedException();
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

    internal class FieldResolverBinding
    {
        public FieldResolverBinding(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            TypeName = typeName;
        }

        public string TypeName { get; }
    }

    internal class DelegateFieldResolverBinding
        : FieldResolverBinding
    {
        public DelegateFieldResolverBinding(
            string typeName, string fieldName,
            FieldResolverDelegate resolver)
            : base(typeName)
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
            : base(typeName)
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
