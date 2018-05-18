using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Configuration
{
    internal class SchemaConfiguration
        : ISchemaConfiguration
    {
        private readonly Dictionary<string, ScalarType> _scalarTypes =
            new Dictionary<string, ScalarType>();
        private readonly List<ResolverBindingInfo> _resolverBindings =
            new List<ResolverBindingInfo>();
        private readonly List<TypeBindingInfo> _typeBindings =
            new List<TypeBindingInfo>();

        public IBindResolverDelegate BindResolver(
            AsyncFieldResolverDelegate fieldResolver)
        {
            ResolverDelegateBindingInfo bindingInfo =
                new ResolverDelegateBindingInfo
                {
                    AsyncFieldResolver = fieldResolver
                };
            _resolverBindings.Add(bindingInfo);
            return new BindResolverDelegate(bindingInfo);
        }

        public IBindResolverDelegate BindResolver(
            FieldResolverDelegate fieldResolver)
        {
            ResolverDelegateBindingInfo bindingInfo =
                new ResolverDelegateBindingInfo
                {
                    FieldResolver = fieldResolver
                };
            _resolverBindings.Add(bindingInfo);
            return new BindResolverDelegate(bindingInfo);
        }

        public IBindResolver<TResolver> BindResolver<TResolver>()
            where TResolver : class
        {
            return BindResolver<TResolver>(BindingBehavior.Implicit);
        }

        public IBindResolver<TResolver> BindResolver<TResolver>(
            BindingBehavior bindingBehavior)
            where TResolver : class
        {
            ResolverCollectionBindingInfo bindingInfo =
                new ResolverCollectionBindingInfo
                {
                    Behavior = bindingBehavior,
                    ResolverType = typeof(TResolver)
                };
            _resolverBindings.Add(bindingInfo);
            return new BindResolver<TResolver>(bindingInfo);
        }

        public IBindType<T> BindType<T>()
            where T : class
        {
            return BindType<T>(BindingBehavior.Implicit);
        }

        public IBindType<T> BindType<T>(BindingBehavior bindingBehavior)
            where T : class
        {
            TypeBindingInfo bindingInfo = new TypeBindingInfo
            {
                Behavior = bindingBehavior
            };
            _typeBindings.Add(bindingInfo);
            return new BindType<T>(bindingInfo);
        }

        public void RegisterScalar<T>(T scalarType)
            where T : ScalarType
        {
            if (scalarType == null)
            {
                throw new ArgumentNullException(nameof(scalarType));
            }

            _scalarTypes[scalarType.Name] = scalarType;
        }

        public void RegisterScalar<T>()
            where T : ScalarType, new()
        {
            RegisterScalar(new T());
        }

        public void Commit(SchemaContext schemaContext)
        {
            // Complete Binding Objects
            List<TypeBindingInfo> handledTypeBindings = new List<TypeBindingInfo>();
            CompleteCollectionBindings(handledTypeBindings);

            RegisterCustomScalarTypes(schemaContext);
            schemaContext.RegisterResolvers(CreateFieldResolvers(schemaContext));
        }

        private void RegisterCustomScalarTypes(SchemaContext schemaContext)
        {
            foreach (ScalarType scalarType in _scalarTypes.Values)
            {
                schemaContext.RegisterType(scalarType);
            }
        }

        private void CompleteCollectionBindings(List<TypeBindingInfo> handledTypeBindings)
        {
            foreach (ResolverCollectionBindingInfo binding in _resolverBindings
                .OfType<ResolverCollectionBindingInfo>())
            {
                if (binding.ObjectType == null && binding.ObjectTypeName == null)
                {
                    binding.ObjectType = binding.ResolverType;
                }

                TypeBindingInfo typeBinding = null;
                if (binding.ObjectType == null)
                {
                    typeBinding = _typeBindings.FirstOrDefault(
                        t => string.Equals(t.Name, binding.ObjectTypeName,
                            StringComparison.Ordinal));
                    binding.ObjectType = typeBinding?.Type;
                }

                if (binding.ObjectTypeName == null)
                {
                    typeBinding = _typeBindings.FirstOrDefault(
                        t => t.Type == binding.ObjectType);
                    binding.ObjectTypeName = typeBinding?.Name;
                }

                if (binding.ObjectTypeName == null)
                {
                    binding.ObjectTypeName = GetNameFromType(binding.ObjectType);
                }

                CompleteFieldResolverBindungs(binding, typeBinding, binding.Fields);

                if (typeBinding != null)
                {
                    handledTypeBindings.Add(typeBinding);
                }
            }
        }

        private void CompleteFieldResolverBindungs(
            ResolverCollectionBindingInfo resolverCollectionBinding,
            TypeBindingInfo typeBinding,
            IEnumerable<FieldResolverBindungInfo> fieldResolverBindings)
        {
            foreach (FieldResolverBindungInfo binding in
                fieldResolverBindings)
            {
                //if (fieldRes)
            }
        }

        private IEnumerable<FieldResolver> CreateFieldResolvers(SchemaContext schemaContext)
        {
            List<FieldResolver> fieldResolvers = new List<FieldResolver>();

            ResolverCollectionBindingInfo[] collectionBindings = _resolverBindings
                .OfType<ResolverCollectionBindingInfo>().ToArray();
            ResolverBindingContext bindingContext = new ResolverBindingContext(
                schemaContext, _typeBindings, collectionBindings);
            IResolverBindingHandler bindingHandler =
                new ResolverCollectionBindingHandler(bindingContext);

            foreach (ResolverCollectionBindingInfo resolverBinding in collectionBindings)
            {
                fieldResolvers.AddRange(bindingHandler.ApplyBinding(resolverBinding));
            }

            bindingHandler = new ResolverDelegateBindingHandler();

            foreach (ResolverDelegateBindingInfo resolverBinding in
                _resolverBindings.OfType<ResolverDelegateBindingInfo>())
            {
                fieldResolvers.AddRange(bindingHandler.ApplyBinding(resolverBinding));
            }

            return fieldResolvers;
        }

        private string GetNameFromType(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute)))
            {
                return type.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }
            return type.Name;
        }

        private class ResolverBindingContext
            : IResolverBindingContext
        {
            private readonly SchemaContext _schemaContext;
            private readonly ILookup<string, TypeBindingInfo> _typeBindings;
            private readonly ILookup<string, ResolverCollectionBindingInfo> _resolverBindings;

            public ResolverBindingContext(
                SchemaContext schemaContext,
                IEnumerable<TypeBindingInfo> typeBindings,
                IEnumerable<ResolverCollectionBindingInfo> resolverBindings)
            {
                if (schemaContext == null)
                {
                    throw new ArgumentNullException(nameof(schemaContext));
                }

                if (typeBindings == null)
                {
                    throw new ArgumentNullException(nameof(typeBindings));
                }

                if (resolverBindings == null)
                {
                    throw new ArgumentNullException(nameof(resolverBindings));
                }

                _schemaContext = schemaContext;
                _typeBindings = typeBindings.ToLookup(t => t.Name);
                _resolverBindings = resolverBindings
                    .ToLookup(t => t.ObjectTypeName);
            }

            public Field LookupField(FieldReference fieldReference)
            {
                IOutputType type = _schemaContext.GetOutputType(
                    fieldReference.TypeName);

                if (type is ObjectType objectType && objectType.Fields
                    .TryGetValue(fieldReference.FieldName, out Field field))
                {
                    return field;
                }
                return null;
            }

            public string LookupFieldName(FieldResolverMember fieldResolverMember)
            {
                foreach (ResolverCollectionBindingInfo resolverBinding in
                    _resolverBindings[fieldResolverMember.TypeName])
                {
                    FieldResolverBindungInfo fieldBinding = resolverBinding.Fields
                        .FirstOrDefault(t => t.FieldMember == fieldResolverMember.Member);
                    if (fieldBinding != null)
                    {
                        return fieldBinding.FieldName;
                    }
                }

                TypeBindingInfo binding = _typeBindings[fieldResolverMember.TypeName].FirstOrDefault();
                if (binding == null)
                {
                    FieldBindingInfo fieldBinding = binding.Fields
                        .FirstOrDefault(t => t.Member == fieldResolverMember.Member);
                    if (fieldBinding != null)
                    {
                        return fieldBinding.Name;
                    }
                }

                return fieldResolverMember.FieldName;
            }
        }
    }
}
