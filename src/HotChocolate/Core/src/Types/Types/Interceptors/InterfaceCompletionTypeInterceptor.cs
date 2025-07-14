using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class InterfaceCompletionTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<TypeSystemObject, TypeInfo> _typeInfos = [];
    private readonly Dictionary<Type, TypeInfo> _allInterfaceRuntimeTypes = [];
    private readonly HashSet<Type> _interfaceRuntimeTypes = [];
    private readonly HashSet<string> _completed = [];
    private readonly HashSet<string> _completedFields = [];
    private readonly Queue<InterfaceType> _backlog = new();

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        TypeSystemConfiguration configuration)
    {
        // we need to preserve the initialization context of all
        // interface types and object types.
        if (configuration is IComplexOutputTypeConfiguration typeDefinition)
        {
            _typeInfos.Add(discoveryContext.Type, new(discoveryContext, typeDefinition));
        }
    }

    public override void OnTypesInitialized()
    {
        // after all types have been initialized we will index the runtime
        // types of all interfaces.
        foreach (var interfaceTypeInfo in _typeInfos.Values
            .Where(t => t.Configuration.RuntimeType is { } rt
                && rt != typeof(object)
                && t.Configuration is InterfaceTypeConfiguration))
        {
            if (!_allInterfaceRuntimeTypes.ContainsKey(interfaceTypeInfo.Configuration.RuntimeType))
            {
                _allInterfaceRuntimeTypes.Add(
                    interfaceTypeInfo.Configuration.RuntimeType,
                    interfaceTypeInfo);
            }
        }

        // we now will use the runtime types to infer interface usage ...
        foreach (var typeInfo in _typeInfos.Values.Where(IsRelevant))
        {
            _interfaceRuntimeTypes.Clear();

            TryInferInterfaceFromRuntimeType(
                GetRuntimeType(typeInfo),
                _allInterfaceRuntimeTypes.Keys,
                _interfaceRuntimeTypes);

            if (_interfaceRuntimeTypes.Count > 0)
            {
                // if we detect that this type implements an interface,
                // we will register it as a dependency.
                foreach (var interfaceRuntimeType in _interfaceRuntimeTypes)
                {
                    var interfaceTypeInfo = _allInterfaceRuntimeTypes[interfaceRuntimeType];
                    var interfaceTypeDependency = new TypeDependency(
                        interfaceTypeInfo.Context.TypeReference,
                        TypeDependencyFulfilled.Completed);

                    typeInfo.Context.Dependencies.Add(interfaceTypeDependency);
                    typeInfo.Configuration.Interfaces.Add(interfaceTypeDependency.Type);
                }
            }
        }
    }

    // defines if this type has a concrete runtime type.
    private bool IsRelevant(TypeInfo typeInfo)
    {
        if (typeInfo.Configuration is ObjectTypeConfiguration { IsExtension: true } objectDef
            && objectDef.FieldBindingType != typeof(object))
        {
            return true;
        }

        var runtimeType = typeInfo.Configuration.RuntimeType;
        return runtimeType != typeof(object);
    }

    private Type GetRuntimeType(TypeInfo typeInfo)
    {
        if (typeInfo.Configuration is ObjectTypeConfiguration { IsExtension: true } objectDef)
        {
            return objectDef.FieldBindingType ?? typeof(object);
        }

        return typeInfo.Configuration.RuntimeType;
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        TypeSystemConfiguration configuration)
    {
        if (configuration is InterfaceTypeConfiguration { Interfaces: { Count: > 0 } } typeDef)
        {
            _completed.Clear();
            _completedFields.Clear();
            _backlog.Clear();

            foreach (var interfaceRef in typeDef.Interfaces)
            {
                if (completionContext.TryGetType(
                    interfaceRef,
                    out InterfaceType? interfaceType))
                {
                    _completed.Add(interfaceType.Name);
                    _backlog.Enqueue(interfaceType);
                }
            }

            foreach (var field in typeDef.Fields)
            {
                _completedFields.Add(field.Name);
            }

            CompleteInterfacesAndFields(typeDef);
        }

        if (configuration is ObjectTypeConfiguration { Interfaces: { Count: > 0 } } objectTypeDef)
        {
            _completed.Clear();
            _completedFields.Clear();
            _backlog.Clear();

            foreach (var interfaceRef in objectTypeDef.Interfaces)
            {
                if (completionContext.TryGetType(
                    interfaceRef,
                    out InterfaceType? interfaceType))
                {
                    _completed.Add(interfaceType.Name);
                    _backlog.Enqueue(interfaceType);
                }
            }

            foreach (var field in objectTypeDef.Fields)
            {
                _completedFields.Add(field.Name);
            }

            CompleteInterfacesAndFields(objectTypeDef);
        }
    }

    private void CompleteInterfacesAndFields(IComplexOutputTypeConfiguration definition)
    {
        while (_backlog.Count > 0)
        {
            var current = _backlog.Dequeue();
            var typeInfo = _typeInfos[current];
            definition.Interfaces.Add(TypeReference.Create(current));

            if (definition is InterfaceTypeConfiguration interfaceDef)
            {
                foreach (var field in ((InterfaceTypeConfiguration)typeInfo.Configuration).Fields)
                {
                    if (_completedFields.Add(field.Name))
                    {
                        interfaceDef.Fields.Add(field);
                    }
                }
            }

            foreach (var interfaceType in current.Implements)
            {
                if (_completed.Add(interfaceType.Name))
                {
                    _backlog.Enqueue(interfaceType);
                }
            }
        }
    }

    private static void TryInferInterfaceFromRuntimeType(
        Type runtimeType,
        ICollection<Type> allInterfaces,
        ICollection<Type> interfaces)
    {
        if (runtimeType == typeof(object))
        {
            return;
        }

        foreach (var interfaceType in runtimeType.GetInterfaces())
        {
            if (allInterfaces.Contains(interfaceType))
            {
                interfaces.Add(interfaceType);
            }
        }
    }

    private readonly struct TypeInfo(
        ITypeDiscoveryContext context,
        IComplexOutputTypeConfiguration configuration)
    {
        public ITypeDiscoveryContext Context { get; } = context;

        public IComplexOutputTypeConfiguration Configuration { get; } = configuration;

        public override string ToString() => Configuration.Name;
    }
}
