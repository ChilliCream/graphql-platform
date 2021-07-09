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
        private readonly Dictionary<NameString, ObjectFieldDefinition> _fields = new();
        private readonly Dictionary<NameString, Type> _runtimeTypes = new();
        private readonly Dictionary<FieldCoordinate, MemberInfo> _members = new();
        private readonly Dictionary<NameString, List<Type>> _resolverTypes;
        private readonly List<FieldResolverConfig> _fieldResolvers;
        private ILookup<NameString, FieldResolverConfig> _configs = default!;

        public ResolverTypeInterceptor(
            List<FieldResolverConfig> fieldResolvers,
            Dictionary<NameString, List<Type>> resolverTypes)
        {
            _fieldResolvers = fieldResolvers;
            _resolverTypes = resolverTypes;
        }

        public override bool CanHandle(ITypeSystemObjectContext context)
            => _fieldResolvers.Count > 0;

        public override void OnAfterCompleteTypeNames()
        {
            _configs = _fieldResolvers.ToLookup(t => t.Field.TypeName);
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ObjectType type &&
                definition is ObjectTypeDefinition typeDefinition &&
                _configs.Contains(type.Name))
            {
                foreach (ObjectFieldDefinition field in typeDefinition.Fields)
                {
                    _fields[field.Name] = field;
                }

                foreach (FieldResolverConfig config in _configs[type.Name])
                {
                    if (_fields.TryGetValue(config.Field.FieldName, out var field))
                    {
                        field.Resolvers = config.ToFieldResolverDelegates();
                    }
                }

                _fields.Clear();
            }
        }

        private void CollectResolverMembers(
            ITypeInspector typeInspector,
            INamingConventions naming,
            NameString typeName)
        {
            if (_resolverTypes.TryGetValue(typeName, out List<Type>? resolverTypes))
            {
                foreach (var resolverType in resolverTypes)
                {
                    foreach (var member in typeInspector.GetMembers(resolverType, false))
                    {
                        NameString fieldName = naming.GetMemberName(member, MemberKind.ObjectField);
                        var field = new FieldCoordinate(typeName, fieldName);
                        _members[field] = member;
                    }
                }
            }
        }

        private void TryResolveRuntimeType(
            ITypeCompletionContext context,
            IReadOnlyCollection<ObjectTypeDefinition> typeDefinitions)
        {
            var missingRuntimeType = new HashSet<ObjectTypeDefinition>();

            foreach (ObjectTypeDefinition typeDefinition in typeDefinitions)
            {
                if (typeDefinition.RuntimeType == typeof(object))
                {
                    missingRuntimeType.Add(typeDefinition);
                }
                else
                {
                    _runtimeTypes[typeDefinition.Name] = typeDefinition.RuntimeType;
                }
            }

            if (missingRuntimeType.Count == 0)
            {
                return;
            }

            var fieldTypes = new List<(ObjectTypeDefinition, FieldCoordinate, IType)>();

            foreach (ObjectTypeDefinition typeDefinition in typeDefinitions)
            {
                foreach (ObjectFieldDefinition field in typeDefinition.Fields)
                {
                    if (field.Type is not null &&
                        context.TryGetType(field.Type, out IType type) &&
                        type.NamedType() is ObjectType { Definition: not null } objectType)
                    {
                        var fieldCoordinate = new FieldCoordinate(typeDefinition.Name, field.Name);
                        fieldTypes.Add((objectType.Definition, fieldCoordinate, type));
                    }
                }
            }

            ILookup<ObjectTypeDefinition, (FieldCoordinate, IType)> fieldTypeLookup =
                fieldTypes.ToLookup(t => t.Item1, t => (t.Item2, t.Item3));

            foreach (ObjectTypeDefinition type in missingRuntimeType)
            {
                foreach ((FieldCoordinate Field, IType Type) fieldInfo in fieldTypeLookup[type])
                {
                    FieldResolverConfig resolverConfig =
                        _configs[fieldInfo.Field.TypeName].FirstOrDefault(
                            t => t.Field.Equals(fieldInfo.Field));

                    if (!resolverConfig.IsDefault &&
                        resolverConfig.ResultType != typeof(object) &&
                        Unwrap(context, resolverConfig.ResultType, fieldInfo.Type) is { } rt)
                    {
                        type.RuntimeType = rt;
                        break;
                    }

                    if (_members.TryGetValue(fieldInfo.Field, out MemberInfo? member))
                    {
                        IExtendedType extendedType = context.TypeInspector.GetReturnType(member);
                        if (Unwrap(context, extendedType, fieldInfo.Type) is { } rt2)
                        {
                            type.RuntimeType = rt2;
                            break;
                        }
                    }
                }
            }
        }

        private static Type? Unwrap(
            ITypeCompletionContext context,
            Type resultType,
            IType type)
            => Unwrap(context, context.TypeInspector.GetType(resultType), type);

        private static Type? Unwrap(
            ITypeCompletionContext context,
            IExtendedType extendedType,
            IType type)
        {
            if (type.IsNonNullType())
            {
                return Unwrap(context, extendedType, type.InnerType());
            }

            if (type.IsListType())
            {
                if (extendedType.ElementType is null)
                {
                    return null;
                }

                return Unwrap(context, extendedType.ElementType, type.InnerType());
            }

            return extendedType.IsNullable
                ? context.TypeInspector.ChangeNullability(extendedType, false).Source
                : extendedType.Source;
        }
    }
}
