using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly List<Func<ISchemaContext, INamedType>> _typeFactories =
            new List<Func<ISchemaContext, INamedType>>();

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
                Behavior = bindingBehavior,
                Type = typeof(T)
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

        public void RegisterType<T>(params Func<ISchemaContext, T>[] typeFactory)
            where T : INamedTypeConfig
        {
            if (typeFactory == null)
            {
                throw new ArgumentNullException(nameof(typeFactory));
            }

            foreach (Func<ISchemaContext, T> factory in typeFactory)
            {
                Func<ISchemaContext, INamedType> namedTypeFactory;
                if (typeof(EnumTypeConfig).IsAssignableFrom(typeof(T)))
                {
                    namedTypeFactory = c => new EnumType(
                        (EnumTypeConfig)(object)factory(c));
                }
                else if (typeof(InputObjectTypeConfig).IsAssignableFrom(typeof(T)))
                {
                    namedTypeFactory = c => new InputObjectType(
                        (InputObjectTypeConfig)(object)factory(c));
                }
                else if (typeof(InterfaceTypeConfig).IsAssignableFrom(typeof(T)))
                {
                    namedTypeFactory = c => new InterfaceType(
                        (InterfaceTypeConfig)(object)factory(c));
                }
                else if (typeof(ObjectTypeConfig).IsAssignableFrom(typeof(T)))
                {
                    namedTypeFactory = c => new ObjectType(
                        (ObjectTypeConfig)(object)factory(c));
                }
                else if (typeof(UnionTypeConfig).IsAssignableFrom(typeof(T)))
                {
                    namedTypeFactory = c => new UnionType(
                        (UnionTypeConfig)(object)factory(c));
                }
                else
                {
                    throw new NotSupportedException(
                        $"The {typeof(T).Name} type configuration " +
                        "is not yet supported.");
                }
                _typeFactories.Add(namedTypeFactory);
            }
        }

        internal void Commit(SchemaContext schemaContext)
        {
            if (schemaContext == null)
            {
                throw new ArgumentNullException(nameof(schemaContext));
            }

            // register custom scalars and enums
            RegisterCustomScalarTypes(schemaContext);
            RegisterCustomTypes(schemaContext);

            // create type bindings
            Dictionary<string, ObjectTypeBinding> objectTypeBindings =
                CreateObjectTypeBindings(schemaContext);
            Dictionary<string, InputObjectTypeBinding> inputObjectTypeBindings =
                CreateInputObjectTypeBindings(schemaContext);
            // todo : register type bindings with context

            // complete resolver bindings for further processing
            List<ObjectTypeBinding> handledTypeBindings = new List<ObjectTypeBinding>();
            CompleteDelegateBindings(objectTypeBindings, handledTypeBindings);
            CompleteCollectionBindings(objectTypeBindings, handledTypeBindings);

            // create field resolvers and register them
            List<FieldResolver> fieldResolvers = new List<FieldResolver>();
            fieldResolvers.AddRange(CreateFieldResolvers(schemaContext));
            fieldResolvers.AddRange(CreateMissingResolvers(
                schemaContext, fieldResolvers, objectTypeBindings));
            schemaContext.RegisterResolvers(fieldResolvers);
        }

        private void RegisterCustomScalarTypes(SchemaContext schemaContext)
        {
            foreach (ScalarType scalarType in _scalarTypes.Values)
            {
                schemaContext.RegisterType(scalarType);
            }
        }

        private void RegisterCustomTypes(SchemaContext schemaContext)
        {
            foreach (Func<ISchemaContext, INamedType> factory in _typeFactories)
            {
                schemaContext.RegisterType(factory(schemaContext));
            }
        }

        // object type bindings
        private Dictionary<string, ObjectTypeBinding> CreateObjectTypeBindings(
            SchemaContext schemaContext)
        {
            Dictionary<string, ObjectTypeBinding> typeBindings =
                new Dictionary<string, ObjectTypeBinding>();

            foreach (TypeBindingInfo typeBindingInfo in _typeBindings)
            {
                if (typeBindingInfo.Name == null)
                {
                    typeBindingInfo.Name = GetNameFromType(typeBindingInfo.Type);
                }

                IEnumerable<FieldBinding> fieldBindings = null;
                if (schemaContext.TryGetOutputType<ObjectType>(
                    typeBindingInfo.Name, out ObjectType ot))
                {
                    fieldBindings = CreateFieldBindings(typeBindingInfo, ot.Fields);
                    typeBindings[ot.Name] = new ObjectTypeBinding(ot.Name,
                        typeBindingInfo.Type, ot, fieldBindings);
                }
            }

            return typeBindings;
        }

        private IEnumerable<FieldBinding> CreateFieldBindings(
            TypeBindingInfo typeBindingInfo,
            IReadOnlyDictionary<string, Field> fields)
        {
            Dictionary<string, FieldBinding> fieldBindings =
                new Dictionary<string, FieldBinding>();

            // create explicit field bindings
            foreach (FieldBindingInfo fieldBindingInfo in
                typeBindingInfo.Fields)
            {
                if (fieldBindingInfo.Name == null)
                {
                    fieldBindingInfo.Name = GetNameFromMember(
                        fieldBindingInfo.Member);
                }

                if (fields.TryGetValue(fieldBindingInfo.Name, out Field field))
                {
                    fieldBindings[field.Name] = new FieldBinding(
                        fieldBindingInfo.Name, fieldBindingInfo.Member, field);
                }
            }

            // create implicit field bindings
            if (typeBindingInfo.Behavior == BindingBehavior.Implicit)
            {
                Dictionary<string, MemberInfo> members =
                    GetMembers(typeBindingInfo.Type);
                foreach (Field field in fields.Values
                    .Where(t => !fieldBindings.ContainsKey(t.Name)))
                {
                    if (members.TryGetValue(field.Name, out MemberInfo member))
                    {
                        fieldBindings[field.Name] = new FieldBinding(
                            field.Name, member, field);
                    }
                }
            }

            return fieldBindings.Values;
        }

        // input object type bindings
        private Dictionary<string, InputObjectTypeBinding> CreateInputObjectTypeBindings(
           SchemaContext schemaContext)
        {
            Dictionary<string, InputObjectTypeBinding> typeBindings =
                new Dictionary<string, InputObjectTypeBinding>();

            foreach (TypeBindingInfo typeBindingInfo in _typeBindings)
            {
                if (typeBindingInfo.Name == null)
                {
                    typeBindingInfo.Name = GetNameFromType(typeBindingInfo.Type);
                }

                IEnumerable<InputFieldBinding> fieldBindings = null;
                if (schemaContext.TryGetInputType<InputObjectType>(
                    typeBindingInfo.Name, out InputObjectType iot))
                {
                    fieldBindings = CreateInputFieldBindings(typeBindingInfo, iot.Fields);
                    typeBindings[iot.Name] = new InputObjectTypeBinding(iot.Name,
                        typeBindingInfo.Type, iot, fieldBindings);
                }
            }

            return typeBindings;
        }

        private IEnumerable<InputFieldBinding> CreateInputFieldBindings(
            TypeBindingInfo typeBindingInfo,
            IReadOnlyDictionary<string, InputField> fields)
        {
            Dictionary<string, InputFieldBinding> fieldBindings =
                new Dictionary<string, InputFieldBinding>();

            // create explicit field bindings
            foreach (FieldBindingInfo fieldBindingInfo in
                typeBindingInfo.Fields)
            {
                if (fieldBindingInfo.Name == null)
                {
                    fieldBindingInfo.Name = GetNameFromMember(
                        fieldBindingInfo.Member);
                }

                if (fields.TryGetValue(fieldBindingInfo.Name, out InputField field))
                {
                    if (fieldBindingInfo.Member is PropertyInfo p)
                    {
                        fieldBindings[field.Name] = new InputFieldBinding(
                            fieldBindingInfo.Name, p, field);
                    }
                    // TODO : error -> exception?
                }
            }

            // create implicit field bindings
            if (typeBindingInfo.Behavior == BindingBehavior.Implicit)
            {
                Dictionary<string, PropertyInfo> properties =
                    GetProperties(typeBindingInfo.Type);
                foreach (InputField field in fields.Values
                    .Where(t => !fieldBindings.ContainsKey(t.Name)))
                {
                    if (properties.TryGetValue(field.Name,
                        out PropertyInfo property))
                    {
                        fieldBindings[field.Name] = new InputFieldBinding(
                            field.Name, property, field);
                    }
                }
            }

            return fieldBindings.Values;
        }

        // complete resolver bindings
        private void CompleteDelegateBindings(
            Dictionary<string, ObjectTypeBinding> typeBindings,
            List<ObjectTypeBinding> handledTypeBindings)
        {
            foreach (ResolverDelegateBindingInfo binding in _resolverBindings
                .OfType<ResolverDelegateBindingInfo>())
            {
                if (binding.ObjectTypeName == null && binding.ObjectType == null)
                {
                    // skip incomplete binding --> todo: maybe an exception?
                    continue;
                }

                if (binding.ObjectTypeName == null)
                {
                    ObjectTypeBinding typeBinding = typeBindings.Values
                        .FirstOrDefault(t => t.Type == binding.ObjectType);
                    FieldBinding fieldBinding = typeBinding?.Fields.Values
                        .FirstOrDefault(t => t.Member == binding.FieldMember);
                    binding.ObjectTypeName = typeBinding?.Name;
                    binding.FieldName = fieldBinding?.Name;
                }
            }
        }

        private void CompleteCollectionBindings(
            Dictionary<string, ObjectTypeBinding> typeBindings,
            List<ObjectTypeBinding> handledTypeBindings)
        {
            foreach (ResolverCollectionBindingInfo binding in _resolverBindings
                .OfType<ResolverCollectionBindingInfo>())
            {
                if (binding.ObjectType == null && binding.ObjectTypeName == null)
                {
                    binding.ObjectType = binding.ResolverType;
                }

                ObjectTypeBinding typeBinding = null;
                if (binding.ObjectType == null && typeBindings
                    .TryGetValue(binding.ObjectTypeName, out typeBinding))
                {
                    binding.ObjectType = typeBinding.Type;
                }

                if (binding.ObjectTypeName == null)
                {
                    typeBinding = typeBindings.Values.FirstOrDefault(
                        t => t.Type == binding.ObjectType);
                    binding.ObjectTypeName = typeBinding?.Name;
                }

                if (binding.ObjectTypeName == null)
                {
                    binding.ObjectTypeName = GetNameFromType(binding.ObjectType);
                }

                // TODO : error handling if object type cannot be resolverd

                CompleteFieldResolverBindungs(binding, typeBinding, binding.Fields);

                if (typeBinding != null)
                {
                    handledTypeBindings.Add(typeBinding);
                }
            }
        }

        private void CompleteFieldResolverBindungs(
            ResolverCollectionBindingInfo resolverCollectionBinding,
            ObjectTypeBinding typeBinding,
            IEnumerable<FieldResolverBindungInfo> fieldResolverBindings)
        {
            foreach (FieldResolverBindungInfo binding in
                fieldResolverBindings)
            {
                if (binding.FieldMember == null && binding.FieldName == null)
                {
                    binding.FieldMember = binding.ResolverMember;
                }

                if (binding.FieldMember == null && typeBinding != null
                    && typeBinding.Fields.TryGetValue(
                        binding.FieldName, out FieldBinding fieldBinding))
                {
                    binding.FieldMember = fieldBinding.Member;
                }

                if (binding.FieldName == null && typeBinding != null)
                {
                    fieldBinding = typeBinding.Fields.Values
                        .FirstOrDefault(t => t.Member == binding.FieldMember);
                    binding.FieldName = fieldBinding?.Name;
                }

                // todo : error handling
                if (binding.FieldName == null)
                {
                    binding.FieldName = GetNameFromMember(binding.FieldMember);
                }
            }
        }

        private IEnumerable<FieldResolver> CreateFieldResolvers(
            SchemaContext schemaContext)
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

        private IEnumerable<FieldResolver> CreateMissingResolvers(
            SchemaContext schemaContext,
            IEnumerable<FieldResolver> fieldResolvers,
            Dictionary<string, ObjectTypeBinding> typeBindings)
        {
            Dictionary<FieldReference, FieldResolver> lookupField =
                fieldResolvers.ToDictionary(
                    t => new FieldReference(t.TypeName, t.FieldName));

            FieldResolverDiscoverer discoverer = new FieldResolverDiscoverer();
            List<FieldResolverDescriptor> descriptors = new List<FieldResolverDescriptor>();
            foreach (ObjectTypeBinding typeBinding in typeBindings.Values)
            {
                List<FieldResolverMember> missingResolvers = new List<FieldResolverMember>();
                foreach (FieldBinding field in typeBinding.Fields.Values)
                {
                    missingResolvers.Add(new FieldResolverMember(
                        typeBinding.Name, field.Name, field.Member));
                }
                descriptors.AddRange(discoverer.GetSelectedResolvers(
                    typeBinding.Type, typeBinding.Type, missingResolvers));
            }

            FieldResolverBuilder fieldResolverBuilder = new FieldResolverBuilder();
            return fieldResolverBuilder.Build(descriptors);
        }

        private Dictionary<string, MemberInfo> GetMembers(Type type)
        {
            Dictionary<string, MemberInfo> members =
                new Dictionary<string, MemberInfo>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in type.GetProperties())
            {
                members[GetNameFromMember(property)] = property;
            }

            foreach (MethodInfo method in type.GetMethods())
            {
                members[GetNameFromMember(method)] = method;
                if (method.Name.Length > 3 && method.Name
                    .StartsWith("Get", StringComparison.OrdinalIgnoreCase))
                {
                    members[method.Name.Substring(3)] = method;
                }
            }

            return members;
        }

        private Dictionary<string, PropertyInfo> GetProperties(Type type)
        {
            Dictionary<string, PropertyInfo> members =
                new Dictionary<string, PropertyInfo>(
                    StringComparer.OrdinalIgnoreCase);

            foreach (PropertyInfo property in type.GetProperties())
            {
                members[GetNameFromMember(property)] = property;
            }

            return members;
        }

        private string GetNameFromType(Type type)
        {
            if (type.IsDefined(typeof(GraphQLNameAttribute)))
            {
                return type.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }
            return type.Name;
        }

        private string GetNameFromMember(MemberInfo member)
        {
            if (member.IsDefined(typeof(GraphQLNameAttribute)))
            {
                return member.GetCustomAttribute<GraphQLNameAttribute>().Name;
            }

            if (member.Name.Length == 1)
            {
                return member.Name.ToLowerInvariant();
            }

            return member.Name.Substring(0, 1).ToLowerInvariant() + member.Name.Substring(1);
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
                if (binding != null)
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
