using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Interceptors
{
    internal sealed class ResolverTypeInterceptor : TypeInterceptor
    {
        private readonly Dictionary<NameString, ITypeDefinition> _typeDefs = new();
        private readonly List<FieldResolverConfig> _fieldResolvers;
        private readonly List<(NameString, Type)> _resolverTypeList;
        private readonly Dictionary<NameString, Type> _runtimeTypes;
        private IDescriptorContext _context = default!;
        private INamingConventions _naming = default!;
        private ITypeInspector _typeInspector = default!;
        private IResolverCompiler _resolverCompiler = default!;
        private TypeReferenceResolver _typeReferenceResolver = default!;
        private ILookup<NameString, Type> _resolverTypes = default!;
        private ILookup<NameString, FieldResolverConfig> _configs = default!;

        public ResolverTypeInterceptor(
            List<FieldResolverConfig> fieldResolvers,
            List<(NameString, Type)> resolverTypes,
            Dictionary<NameString, Type> runtimeTypes)
        {
            _fieldResolvers = fieldResolvers;
            _resolverTypeList = resolverTypes;
            _runtimeTypes = runtimeTypes;
        }

        public override bool CanHandle(ITypeSystemObjectContext context) => true;

        internal override void InitializeContext(
            IDescriptorContext context,
            TypeReferenceResolver typeReferenceResolver)
        {
            _context = context;
            _naming = context.Naming;
            _typeInspector = context.TypeInspector;
            _resolverCompiler = context.ResolverCompiler;
            _typeReferenceResolver = typeReferenceResolver;
            _resolverTypes = _resolverTypeList.ToLookup(t => t.Item1, t => t.Item2);
            _configs = _fieldResolvers.ToLookup(t => t.Field.TypeName);
        }

        public override void OnAfterCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (!completionContext.IsIntrospectionType &&
                completionContext.Type is INamedType namedType &&
                definition is ITypeDefinition typeDef)
            {
                if (_runtimeTypes.TryGetValue(typeDef.Name, out Type? type))
                {
                    typeDef.RuntimeType = type;
                }
                _typeDefs.Add(namedType.Name, typeDef);
            }
        }

        public override void OnAfterCompleteTypeNames()
        {
            var resolvers = new Dictionary<NameString, FieldResolverConfig>();
            var members = new Dictionary<NameString, MemberInfo>();
            var values = new Dictionary<NameString, (object, MemberInfo)>();
            var valuesToName = new Dictionary<string, (object, MemberInfo)>();

            ApplyResolver(resolvers, members);
            ApplySourceMembers(members, values, valuesToName);
        }

        private void ApplyResolver(
            Dictionary<NameString, FieldResolverConfig> resolvers,
            Dictionary<NameString, MemberInfo> members)
        {
            var completed = 0;

            foreach (ObjectTypeDefinition objectTypeDef in
                _typeDefs.Values.OfType<ObjectTypeDefinition>())
            {
                if (_configs.Contains(objectTypeDef.Name))
                {
                    foreach (FieldResolverConfig config in _configs[objectTypeDef.Name])
                    {
                        resolvers[config.Field.FieldName] = config;
                    }

                    foreach (ObjectFieldDefinition field in objectTypeDef.Fields)
                    {
                        if (resolvers.TryGetValue(field.Name, out FieldResolverConfig config))
                        {
                            field.Resolvers = config.ToFieldResolverDelegates();
                            TrySetRuntimeType(field, config);
                            completed++;
                        }
                    }

                    resolvers.Clear();
                }

                if (completed < objectTypeDef.Fields.Count)
                {
                    ApplyResolverTypes(objectTypeDef, members);
                }

                members.Clear();
            }
        }

        private void ApplyResolverTypes(
            ObjectTypeDefinition objectTypeDef,
            Dictionary<NameString, MemberInfo> members)
        {
            CollectResolverMembers(objectTypeDef.Name, members);

            if (members.Count > 0)
            {
                foreach (ObjectFieldDefinition field in objectTypeDef.Fields)
                {
                    if (!field.Resolvers.HasResolvers &&
                        members.TryGetValue(field.Name, out MemberInfo? member))
                    {
                        field.ResolverMember = member;

                        field.Resolvers = _resolverCompiler.CompileResolve(
                            member,
                            objectTypeDef.RuntimeType,
                            resolverType: member.ReflectedType);

                        TrySetRuntimeTypeFromMember(field.Type, member);
                    }
                }
            }
        }

        private void ApplySourceMembers(
            Dictionary<NameString, MemberInfo> members,
            Dictionary<NameString, (object, MemberInfo)> values,
            Dictionary<string, (object, MemberInfo)> valuesToName)
        {
            var queue = new Queue<ITypeDefinition>(
                _typeDefs.Values.Where(t => t.RuntimeType != typeof(object)));

            while (queue.Count > 0)
            {
                switch (queue.Dequeue())
                {
                    case ObjectTypeDefinition objectTypeDef:
                        ApplyObjectSourceMembers(objectTypeDef, members, queue);
                        break;

                    case InputObjectTypeDefinition inputTypeDef:
                        ApplyInputSourceMembers(inputTypeDef, members, queue);
                        break;

                    case EnumTypeDefinition enumTypeDef:
                        ApplyEnumSourceMembers(enumTypeDef, values, valuesToName);
                        break;
                }
            }
        }

        private void ApplyObjectSourceMembers(
            ObjectTypeDefinition objectTypeDef,
            Dictionary<NameString, MemberInfo> members,
            Queue<ITypeDefinition> typesToAnalyze)
        {
            var initialized = false;

            foreach (ObjectFieldDefinition field in objectTypeDef.Fields)
            {
                if (!initialized && field.Member is null)
                {
                    CollectSourceMembers(objectTypeDef.RuntimeType, members);
                    initialized = true;
                }

                if (field.Member is null && members.TryGetValue(field.Name, out MemberInfo? member))
                {
                    field.Member = member;
                }

                if (field.Member is not null && !field.Resolvers.HasResolvers)
                {
                    field.Resolvers = _resolverCompiler.CompileResolve(
                        field.Member,
                        objectTypeDef.RuntimeType);

                    if (TrySetRuntimeTypeFromMember(field.Type, field.Member) is { } updated)
                    {
                        typesToAnalyze.Enqueue(updated);
                    }
                }
            }

            members.Clear();
        }

        private void ApplyInputSourceMembers(
            InputObjectTypeDefinition inputTypeDef,
            Dictionary<NameString, MemberInfo> members,
            Queue<ITypeDefinition> typesToAnalyze)
        {
            var initialized = false;

            foreach (InputFieldDefinition field in inputTypeDef.Fields)
            {
                if (!initialized && field.Property is null)
                {
                    CollectSourceMembers(inputTypeDef.RuntimeType, members);
                    initialized = true;
                }

                if (field.Property is null &&
                    members.TryGetValue(field.Name, out MemberInfo? member) &&
                    member is PropertyInfo property)
                {
                    field.Property = property;

                    if (TrySetRuntimeTypeFromMember(field.Type, property) is { } updated)
                    {
                        typesToAnalyze.Enqueue(updated);
                    }
                }
            }

            members.Clear();
        }

        private void ApplyEnumSourceMembers(
            EnumTypeDefinition enumTypeDef,
            Dictionary<NameString, (object, MemberInfo)> values,
            Dictionary<string, (object, MemberInfo)> valuesToName)
        {
            var initialized = false;

            foreach (EnumValueDefinition enumValue in enumTypeDef.Values)
            {
                if (!initialized && enumValue.Member is null)
                {
                    foreach (object value in _typeInspector.GetEnumValues(enumTypeDef.RuntimeType))
                    {
                        NameString name = _naming.GetEnumValueName(value);
                        MemberInfo? member = _typeInspector.GetEnumValueMember(enumTypeDef);
                        values.Add(name, (value, member!));
                        valuesToName.Add(value.ToString()!, (value, member!));
                    }
                    initialized = true;
                }

                (object Value, MemberInfo Member) info;
                if (enumValue.Member is null &&
                    (enumValue.BindTo is null && values.TryGetValue(enumValue.Name, out info) ||
                     enumValue.BindTo is { } b && valuesToName.TryGetValue(b, out info)))
                {
                    enumValue.RuntimeValue = info.Value;
                    enumValue.Member = info.Member;
                }

            }

            values.Clear();
        }

        private void CollectResolverMembers(
            NameString typeName,
            Dictionary<NameString, MemberInfo> members)
        {
            if (!_resolverTypes.Contains(typeName))
            {
                return;
            }

            foreach (var resolverType in _resolverTypes[typeName])
            {
                CollectSourceMembers(resolverType, members);
            }
        }

        private void CollectSourceMembers(
            Type runtimeType,
            Dictionary<NameString, MemberInfo> members)
        {
            foreach (var member in _typeInspector.GetMembers(runtimeType, false))
            {
                NameString name = _naming.GetMemberName(member, MemberKind.ObjectField);
                members[name] = member;
            }
        }

        private void TrySetRuntimeType(ObjectFieldDefinition field, FieldResolverConfig config)
        {
            if (config.ResultType != typeof(object) &&
                field.Type is not null &&
                _typeReferenceResolver.TryGetType(field.Type, out IType? type) &&
                type.NamedType() is IHasTypeDefinition definition &&
                definition.Definition is { } typeDef &&
                typeDef.RuntimeType == typeof(object))
            {
                typeDef.RuntimeType = Unwrap(config.ResultType, type);
            }
        }

        private ITypeDefinition? TrySetRuntimeTypeFromMember(
            ITypeReference? typeRef,
            MemberInfo member)
        {
            if (typeRef is not null &&
                _typeReferenceResolver.TryGetType(typeRef, out IType? type) &&
                type.NamedType() is IHasTypeDefinition definition &&
                definition.Definition is { } typeDef &&
                typeDef.RuntimeType == typeof(object))
            {
                typeDef.RuntimeType = Unwrap(_typeInspector.GetReturnType(member), type);
                return typeDef;
            }

            return null;
        }

        private Type? Unwrap(
            Type resultType,
            IType type)
            => Unwrap(_context.TypeInspector.GetType(resultType), type);

        private Type? Unwrap(
            IExtendedType extendedType,
            IType type)
        {
            if (type.IsNonNullType())
            {
                return Unwrap(extendedType, type.InnerType());
            }

            if (type.IsListType())
            {
                if (extendedType.ElementType is null)
                {
                    return null;
                }

                return Unwrap(extendedType.ElementType, type.InnerType());
            }

            return extendedType.IsNullable
                ? _context.TypeInspector.ChangeNullability(extendedType, false).Source
                : extendedType.Source;
        }
    }
}
