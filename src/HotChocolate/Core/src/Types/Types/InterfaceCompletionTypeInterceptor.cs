using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types
{
    internal class InterfaceCompletionTypeInterceptor : TypeInterceptor
    {
        private Dictionary<ITypeSystemObject, TypeInfo> _typeInfos = new();
        private HashSet<Type> _allInterfaceRuntimeTypes = new();
        private HashSet<Type> _interfaceRuntimeTypes = new();
        private HashSet<NameString> _completed = new();
        private HashSet<NameString> _completedFields = new();
        private Queue<InterfaceType> _backlog = new();

        public override bool TriggerAggregations => true;

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            // we need to preserve the initialization context of all
            // interface types and object types.
            if (definition is IComplexOutputTypeDefinition typeDefinition)
            {
                _typeInfos.Add(discoveryContext.Type, new(discoveryContext, typeDefinition));
            }
        }

        public override void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            // after all types have been initialized we will index the runtime
            // types of all interfaces.
            foreach (TypeInfo interfaceTypeInfo in _typeInfos.Values
                .Where(t => t.Definition.RuntimeType is { } rt &&
                    rt != typeof(object) &&
                    t.Definition is InterfaceTypeDefinition))
            {
                _allInterfaceRuntimeTypes.Add(interfaceTypeInfo.Definition.RuntimeType);
            }

            // we now will use the runtime types to infer interface usage ...
            foreach (TypeInfo typeInfo in _typeInfos.Values.Where(IsRelevant))
            {
                _interfaceRuntimeTypes.Clear();

                TryInferInterfaceFromRuntimeType(
                    GetRuntimeType(typeInfo),
                    _allInterfaceRuntimeTypes,
                    _interfaceRuntimeTypes);

                if (_interfaceRuntimeTypes.Count > 0)
                {
                    // if we detect that this type implements an interface,
                    // we will register it as a dependency.
                    foreach (var typeDependency in _interfaceRuntimeTypes.Select(
                        t => new TypeDependency(
                            TypeReference.Create(
                                typeInfo.Context.TypeInspector.GetType(t),
                                TypeContext.Output),
                            TypeDependencyKind.Completed)))
                    {
                        typeInfo.Context.RegisterDependency(typeDependency);
                        typeInfo.Definition.Interfaces.Add(typeDependency.TypeReference);
                    }
                }
            }
        }

        // defines if this type has a concrete runtime type.
        private bool IsRelevant(TypeInfo typeInfo)
        {
            if (typeInfo.Definition is ObjectTypeDefinition { IsExtension: true } objectDef &&
                objectDef.FieldBindingType != typeof(object))
            {
                return true;
            }

            Type? runtimeType = typeInfo.Definition.RuntimeType;
            return runtimeType is not null! && runtimeType != typeof(object);
        }

        private Type GetRuntimeType(TypeInfo typeInfo)
        {
            if (typeInfo.Definition is ObjectTypeDefinition { IsExtension: true } objectDef)
            {
                return objectDef.FieldBindingType;
            }

            return typeInfo.Definition.RuntimeType;
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (definition is InterfaceTypeDefinition { Interfaces: { Count: > 0 } } typeDef)
            {
                _completed.Clear();
                _completedFields.Clear();
                _backlog.Clear();

                foreach (var interfaceRef in typeDef.Interfaces)
                {
                    if (completionContext.TryGetType(interfaceRef, out InterfaceType interfaceType))
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

            if (definition is ObjectTypeDefinition { Interfaces: { Count: > 0 } } objectTypeDef)
            {
                _completed.Clear();
                _completedFields.Clear();
                _backlog.Clear();

                foreach (var interfaceRef in objectTypeDef.Interfaces)
                {
                    if (completionContext.TryGetType(interfaceRef, out InterfaceType interfaceType))
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

        public override void OnTypesCompleted(
            IReadOnlyCollection<ITypeCompletionContext> completionContexts)
        {
            _typeInfos = null!;
            _allInterfaceRuntimeTypes = null!;
            _interfaceRuntimeTypes = null!;
            _completed = null!;
            _completedFields = null!;
            _backlog = null!;
        }

        private void CompleteInterfacesAndFields(IComplexOutputTypeDefinition definition)
        {
            while (_backlog.Count > 0)
            {
                InterfaceType current = _backlog.Dequeue();
                TypeInfo typeInfo = _typeInfos[current];
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
            foreach (Type interfaceType in runtimeType.GetInterfaces())
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

            public override string? ToString() => Definition.Name;
        }
    }
}
