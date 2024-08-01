using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Interceptors;

internal sealed class InterfaceCompletionTypeInterceptor : TypeInterceptor
{
    private readonly Dictionary<ITypeSystemObject, TypeInfo> _typeInfos = new();
    private readonly Dictionary<Type, TypeInfo> _allInterfaceRuntimeTypes = new();
    private readonly HashSet<Type> _interfaceRuntimeTypes = [];
    private readonly HashSet<string> _completed = [];
    private readonly HashSet<string> _completedFields = [];
    private readonly Queue<InterfaceType> _backlog = new();

    public override void OnAfterInitialize(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase definition)
    {
        // we need to preserve the initialization context of all
        // interface types and object types.
        if (definition is IComplexOutputTypeDefinition typeDefinition)
        {
            _typeInfos.Add(discoveryContext.Type, new(discoveryContext, typeDefinition));
        }
    }

    public override void OnTypesInitialized()
    {
        // after all types have been initialized we will index the runtime
        // types of all interfaces.
        foreach (var interfaceTypeInfo in _typeInfos.Values
            .Where(t => t.Definition.RuntimeType is { } rt &&
                rt != typeof(object) &&
                t.Definition is InterfaceTypeDefinition))
        {
            if (!_allInterfaceRuntimeTypes.ContainsKey(interfaceTypeInfo.Definition.RuntimeType))
            {
                _allInterfaceRuntimeTypes.Add(
                    interfaceTypeInfo.Definition.RuntimeType,
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
                    typeInfo.Definition.Interfaces.Add(interfaceTypeDependency.Type);
                }
            }
        }
    }

    // defines if this type has a concrete runtime type.
    private bool IsRelevant(TypeInfo typeInfo)
    {
        if (typeInfo.Definition is ObjectTypeDefinition { IsExtension: true, } objectDef &&
            objectDef.FieldBindingType != typeof(object))
        {
            return true;
        }

        var runtimeType = typeInfo.Definition.RuntimeType;
        return runtimeType != typeof(object);
    }

    private Type GetRuntimeType(TypeInfo typeInfo)
    {
        if (typeInfo.Definition is ObjectTypeDefinition { IsExtension: true, } objectDef)
        {
            return objectDef.FieldBindingType ?? typeof(object);
        }

        return typeInfo.Definition.RuntimeType;
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is InterfaceTypeDefinition { Interfaces: { Count: > 0, }, } typeDef)
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

        if (definition is ObjectTypeDefinition { Interfaces: { Count: > 0, }, } objectTypeDef)
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

    private void CompleteInterfacesAndFields(IComplexOutputTypeDefinition definition)
    {
        while (_backlog.Count > 0)
        {
            var current = _backlog.Dequeue();
            var typeInfo = _typeInfos[current];
            definition.Interfaces.Add(TypeReference.Create(current));

            if (definition is InterfaceTypeDefinition interfaceDef)
            {
                foreach (var field in ((InterfaceTypeDefinition)typeInfo.Definition).Fields)
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

    private readonly struct TypeInfo
    {
        public TypeInfo(
            ITypeDiscoveryContext context,
            IComplexOutputTypeDefinition definition)
        {
            Context = context;
            Definition = definition;
        }

        public ITypeDiscoveryContext Context { get; }

        public IComplexOutputTypeDefinition Definition { get; }

        public override string ToString() => Definition.Name;
    }
}
