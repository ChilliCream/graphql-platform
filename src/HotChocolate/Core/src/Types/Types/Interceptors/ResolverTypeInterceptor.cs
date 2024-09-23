using System.Reflection;
using HotChocolate.Configuration;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Helpers;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class ResolverTypeInterceptor : TypeInterceptor
{
    private readonly List<ITypeDefinition> _typeDefs = [];
    private readonly List<FieldResolverConfig> _fieldResolvers;
    private readonly List<(string, Type)> _resolverTypeList;
    private readonly Dictionary<string, Type> _runtimeTypes;
    private readonly Dictionary<string, ParameterInfo> _parameters = new();
    private IDescriptorContext _context = default!;
    private INamingConventions _naming = default!;
    private ITypeInspector _typeInspector = default!;
    private IResolverCompiler _resolverCompiler = default!;
    private TypeReferenceResolver _typeReferenceResolver = default!;
    private ILookup<string, Type> _resolverTypes = default!;
    private ILookup<string, FieldResolverConfig> _configs = default!;

    public ResolverTypeInterceptor(
        List<FieldResolverConfig> fieldResolvers,
        List<(string, Type)> resolverTypes,
        Dictionary<string, Type> runtimeTypes)
    {
        _fieldResolvers = fieldResolvers;
        _resolverTypeList = resolverTypes;
        _runtimeTypes = runtimeTypes;
    }

    internal override void InitializeContext(
        IDescriptorContext context,
        TypeInitializer typeInitializer,
        TypeRegistry typeRegistry,
        TypeLookup typeLookup,
        TypeReferenceResolver typeReferenceResolver)
    {
        _context = context;
        _naming = context.Naming;
        _typeInspector = context.TypeInspector;
        _resolverCompiler = context.ResolverCompiler;
        _typeReferenceResolver = typeReferenceResolver;
        _resolverTypes = _resolverTypeList.ToLookup(t => t.Item1, t => t.Item2);
        _configs = _fieldResolvers.ToLookup(t => t.FieldCoordinate.Name);
    }

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        if (discoveryContext is { IsIntrospectionType: false, Type: IHasName namedType, } &&
            definition is ITypeDefinition { NeedsNameCompletion: false, } typeDef)
        {
            if (typeDef.RuntimeType == typeof(object) &&
                _runtimeTypes.TryGetValue(typeDef.Name, out var type))
            {
                typeDef.RuntimeType = type;
            }

            typeDef.Name = namedType.Name;
            _typeDefs.Add(typeDef);
        }
    }

    public override IEnumerable<TypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        var context = new CompletionContext(_typeDefs);
        ApplyResolver(context);
        ApplySourceMembers(context);

        var list = new List<TypeDependency>();

        foreach (var typeDef in _typeDefs)
        {
            switch (typeDef)
            {
                case ObjectTypeDefinition otd:
                    TypeDependencyHelper.CollectDependencies(otd, list);
                    break;

                case InterfaceTypeDefinition itd:
                    TypeDependencyHelper.CollectDependencies(itd, list);
                    break;
            }
        }

        return list.Select(t => t.Type);
    }

    public override void OnAfterCompleteName(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (completionContext is { IsIntrospectionType: false, Type: IHasName namedType, } &&
            definition is ITypeDefinition typeDef)
        {
            if (typeDef.RuntimeType == typeof(object) &&
                _runtimeTypes.TryGetValue(typeDef.Name, out var type))
            {
                typeDef.RuntimeType = type;
            }

            typeDef.Name = namedType.Name;
            _typeDefs.Add(typeDef);
        }
    }

    public override void OnAfterCompleteTypeNames()
    {
        var context = new CompletionContext(_typeDefs);
        ApplyResolver(context);
        ApplySourceMembers(context);
    }

    private void ApplyResolver(CompletionContext context)
    {
        var completed = 0;

        foreach (var objectTypeDef in _typeDefs.OfType<ObjectTypeDefinition>())
        {
            if (_configs.Contains(objectTypeDef.Name))
            {
                foreach (var config in _configs[objectTypeDef.Name])
                {
                    context.Resolvers[config.FieldCoordinate.MemberName!] = config;
                }

                foreach (var field in objectTypeDef.Fields)
                {
                    if (context.Resolvers.TryGetValue(field.Name, out var conf))
                    {
                        field.Resolvers = conf.ToFieldResolverDelegates();
                        TrySetRuntimeType(context, field, conf);
                        completed++;
                    }
                }

                context.Resolvers.Clear();
            }

            if (completed < objectTypeDef.Fields.Count)
            {
                ApplyResolverTypes(context, objectTypeDef);
            }

            context.Members.Clear();
        }
    }

    private void ApplyResolverTypes(
        CompletionContext context,
        ObjectTypeDefinition objectTypeDef)
    {
        CollectResolverMembers(context, objectTypeDef.Name);

        if (context.Members.Count > 0)
        {
            var map = TypeMemHelper.RentArgumentNameMap();

            foreach (var field in objectTypeDef.Fields)
            {
                if (!field.Resolvers.HasResolvers &&
                    context.Members.TryGetValue(field.Name, out var member))
                {
                    field.ResolverMember = member;

                    ObjectFieldDescriptor.From(_context, field).CreateDefinition();

                    map.Clear();

                    foreach (var argument in field.Arguments)
                    {
                        if (argument.Parameter is not null)
                        {
                            map[argument.Parameter] = argument.Name;
                        }
                    }

                    field.Resolvers = _resolverCompiler.CompileResolve(
                        member,
                        objectTypeDef.RuntimeType,
                        resolverType: member.ReflectedType,
                        argumentNames: map,
                        field.GetParameterExpressionBuilders());

                    TryBindArgumentRuntimeType(field, member);
                    TrySetRuntimeTypeFromMember(context, field.Type, member);
                }
            }

            TypeMemHelper.Return(map);
        }
    }

    private void ApplySourceMembers(CompletionContext context)
    {
        foreach (var definition in
            _typeDefs.Where(t => t.RuntimeType != typeof(object)))
        {
            context.TypesToAnalyze.Enqueue(definition);
        }

        while (context.TypesToAnalyze.Count > 0)
        {
            switch (context.TypesToAnalyze.Dequeue())
            {
                case ObjectTypeDefinition objectTypeDef:
                    ApplyObjectSourceMembers(context, objectTypeDef);
                    break;

                case InputObjectTypeDefinition inputTypeDef:
                    ApplyInputSourceMembers(context, inputTypeDef);
                    break;

                case EnumTypeDefinition enumTypeDef:
                    ApplyEnumSourceMembers(context, enumTypeDef);
                    break;
            }
        }
    }

    private void ApplyObjectSourceMembers(
        CompletionContext context,
        ObjectTypeDefinition objectTypeDef)
    {
        var initialized = false;
        var map = TypeMemHelper.RentArgumentNameMap();

        foreach (var field in objectTypeDef.Fields)
        {
            if (!initialized && field.Member is null)
            {
                CollectSourceMembers(context, objectTypeDef.RuntimeType);
                initialized = true;
            }

            if (field.Member is null &&
                context.Members.TryGetValue(field.Name, out var member))
            {
                field.Member = member;

                TryBindArgumentRuntimeType(field, member);
                ObjectFieldDescriptor.From(_context, field).CreateDefinition();

                if (!field.Resolvers.HasResolvers)
                {
                    map.Clear();

                    foreach (var argument in field.Arguments)
                    {
                        if (argument.Parameter is not null)
                        {
                            map[argument.Parameter] = argument.Name;
                        }
                    }

                    field.Resolvers = _resolverCompiler.CompileResolve(
                        field.Member,
                        objectTypeDef.RuntimeType,
                        argumentNames: map,
                        parameterExpressionBuilders: field.GetParameterExpressionBuilders());

                    if (TrySetRuntimeTypeFromMember(context, field.Type, field.Member) is { } u)
                    {
                        foreach (var updated in u)
                        {
                            context.TypesToAnalyze.Enqueue(updated);
                        }
                    }
                }
            }
        }

        TypeMemHelper.Return(map);
        context.Members.Clear();
    }

    private void ApplyInputSourceMembers(
        CompletionContext context,
        InputObjectTypeDefinition inputTypeDef)
    {
        var initialized = false;

        foreach (var field in inputTypeDef.Fields)
        {
            if (!initialized && field.Property is null)
            {
                CollectSourceMembers(context, inputTypeDef.RuntimeType);
                initialized = true;
            }

            if (field.Property is null &&
                context.Members.TryGetValue(field.Name, out var member) &&
                member is PropertyInfo property)
            {
                field.Property = property;

                if (TrySetRuntimeTypeFromMember(context, field.Type, property) is { } upd)
                {
                    foreach (var updated in upd)
                    {
                        context.TypesToAnalyze.Enqueue(updated);
                    }
                }
            }
        }

        context.Members.Clear();
    }

    private void ApplyEnumSourceMembers(
        CompletionContext context,
        EnumTypeDefinition enumTypeDef)
    {
        var initialized = false;

        foreach (var enumValue in enumTypeDef.Values)
        {
            if (!initialized && enumValue.Member is null)
            {
                foreach (var value in _typeInspector.GetEnumValues(enumTypeDef.RuntimeType))
                {
                    var name = _naming.GetEnumValueName(value);
                    var member = _typeInspector.GetEnumValueMember(enumTypeDef);
                    context.Values.Add(name, (value, member!));
                    context.ValuesToName.Add(value.ToString()!, (value, member!));
                }
                initialized = true;
            }

            (object Value, MemberInfo Member) info;
            if (enumValue.Member is null &&
                (enumValue.BindTo is null &&
                    context.Values.TryGetValue(enumValue.Name, out info) ||
                 enumValue.BindTo is { } b &&
                    context.ValuesToName.TryGetValue(b, out info)))
            {
                enumValue.RuntimeValue = info.Value;
                enumValue.Member = info.Member;
            }
        }

        context.Values.Clear();
        context.ValuesToName.Clear();
    }

    private void CollectResolverMembers(CompletionContext context, string typeName)
    {
        if (!_resolverTypes.Contains(typeName))
        {
            return;
        }

        foreach (var resolverType in _resolverTypes[typeName])
        {
            CollectSourceMembers(context, resolverType);
        }
    }

    private void CollectSourceMembers(CompletionContext context, Type runtimeType)
    {
        foreach (var member in _typeInspector.GetMembers(runtimeType, allowObject: true))
        {
            var name = _naming.GetMemberName(member, MemberKind.ObjectField);
            context.Members[name] = member;
        }
    }

    private void TrySetRuntimeType(
        CompletionContext context,
        ObjectFieldDefinition field,
        FieldResolverConfig config)
    {
        if (config.ResultType != typeof(object) &&
            field.Type is not null &&
            _typeReferenceResolver.TryGetType(field.Type, out var type))
        {
            foreach (var typeDef in context.TypeDefs[type.NamedType().Name])
            {
                if (typeDef.RuntimeType == typeof(object))
                {
                    typeDef.RuntimeType = Unwrap(config.ResultType, type);
                }
            }
        }
    }

    private void TryBindArgumentRuntimeType(
        ObjectFieldDefinition field,
        MemberInfo member)
    {
        if (member is MethodInfo method)
        {
            foreach (var parameter in
                _resolverCompiler.GetArgumentParameters(method.GetParameters()))
            {
                _parameters[parameter.Name!] = parameter;
            }

            foreach (var argument in field.Arguments)
            {
                if (_parameters.TryGetValue(argument.Name, out var parameter))
                {
                    argument.Parameter = parameter;
                    argument.RuntimeType = parameter.ParameterType;

                    if (_typeReferenceResolver.TryGetType(argument.Type!, out var type))
                    {
                        var unwrapped = Unwrap(parameter.ParameterType, type);
                        if (unwrapped is not null)
                        {
                            _runtimeTypes.TryAdd(type.NamedType().Name, unwrapped);
                        }
                    }
                }
            }

            _parameters.Clear();
        }
    }

    private IReadOnlyCollection<ITypeDefinition>? TrySetRuntimeTypeFromMember(
        CompletionContext context,
        TypeReference? typeRef,
        MemberInfo member)
    {
        if (typeRef is not null && _typeReferenceResolver.TryGetType(typeRef, out var type))
        {
            List<ITypeDefinition>? updated = null;
            Type? runtimeType = null;

            foreach (var typeDef in context.TypeDefs[type.NamedType().Name])
            {
                if (typeDef.RuntimeType == typeof(object))
                {
                    updated ??= [];
                    runtimeType ??= Unwrap(_typeInspector.GetReturnType(member), type);
                    typeDef.RuntimeType = runtimeType;
                    updated.Add(typeDef);
                }
            }

            return updated;
        }

        return null;
    }

    private Type? Unwrap(Type resultType, IType type)
        => Unwrap(_context.TypeInspector.GetType(resultType), type);

    private Type? Unwrap(IExtendedType extendedType, IType type)
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

    private sealed class CompletionContext
    {
        public readonly Dictionary<string, FieldResolverConfig> Resolvers = new();
        public readonly Dictionary<string, MemberInfo> Members = new();
        public readonly Dictionary<string, (object, MemberInfo)> Values = new();
        public readonly Dictionary<string, (object, MemberInfo)> ValuesToName = new();
        public readonly Queue<ITypeDefinition> TypesToAnalyze = new();
        public readonly ILookup<string, ITypeDefinition> TypeDefs;

        public CompletionContext(List<ITypeDefinition> typeDefs)
        {
            TypeDefs = typeDefs.ToLookup(t => t.Name);
        }
    }
}
