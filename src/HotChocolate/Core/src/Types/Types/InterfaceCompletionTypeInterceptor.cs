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
        private readonly List<TypeInfo> _typeInfos = new();
        private readonly HashSet<Type> _allInterfaceRuntimeTypes = new();
        private readonly HashSet<Type> _interfaceRuntimeTypes = new();

        private readonly Dictionary<NameString, InterfaceType> _interfaceLookup = new();
        private readonly HashSet<NameString> _completed = new();
        private readonly HashSet<NameString> _completedFields = new();
        private readonly HashSet<InterfaceType> _interfaces = new();
        private readonly Queue<IComplexOutputType> _backlog = new();

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            // we need to preserve the initialization context of all
            // interface types and object types.
            if (discoveryContext.Type is IComplexOutputType type &&
                definition is IComplexOutputTypeDefinition typeDefinition)
            {
                _typeInfos.Add(new(discoveryContext, type, typeDefinition));
            }
        }

        public override void OnTypesInitialized(
            IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
        {
            // after all types have been initialized we will index the runtime
            // types of all interfaces.
            foreach (TypeInfo interfaceTypeInfo in _typeInfos
                .Where(t => t.Definition.RuntimeType is { } rt &&
                    rt != typeof(object) &&
                    t.Definition is InterfaceTypeDefinition))
            {
                _allInterfaceRuntimeTypes.Add(interfaceTypeInfo.Definition.RuntimeType);
            }

            // we now will use the runtime types to infer
            foreach (TypeInfo typeInfo in _typeInfos
                .Where(t => t.Definition.RuntimeType is { } rt && rt != typeof(object)))
            {
                TryInferInterfaceFromRuntimeType(
                    typeInfo.Definition.RuntimeType,
                    _allInterfaceRuntimeTypes,
                    _interfaceRuntimeTypes);

                if (_interfaceRuntimeTypes.Count > 0)
                {
                    typeInfo.Context.RegisterDependencyRange(
                        _interfaceRuntimeTypes.Select(
                            t => new TypeDependency(
                                TypeReference.Create(
                                    typeInfo.Context.TypeInspector.GetType(t),
                                    TypeContext.Output),
                                TypeDependencyKind.Completed)));
                }
            }
        }


        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is InterfaceType { Implements: { Count: > 0 } } type &&
                definition is InterfaceTypeDefinition typeDef)
            {
                _completed.Clear();
                _completedFields.Clear();
                _interfaces.Clear();
                _backlog.Clear();

                CompleteInterfacesHelper.Complete(
                    completionContext,
                    typeDef,
                    typeDef.RuntimeType ?? typeof(object),
                    _interfaces,
                    completionContext.Type,
                    typeDef.SyntaxNode);

                foreach (var interfaceType in _interfaces)
                {
                    _backlog.Enqueue(interfaceType);
                }
            }
        }

        private void CollectInterfaces(IComplexOutputTypeDefinition definition)
        {
            while(_backlog.Count > 0)
            {
                IComplexOutputType current = _backlog.Dequeue();

                foreach (var VARIABLE in _backlog)
                {

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
                if (knownInterfaces.Contains(interfaceType))
                {
                    interfaces.Add(interfaceType);
                }
            }
        }

        private readonly struct TypeInfo
        {
            public TypeInfo(
                ITypeDiscoveryContext context,
                IComplexOutputType type,
                IComplexOutputTypeDefinition definition)
            {
                Context = context;
                Type = type;
                Definition = definition;
            }

            public ITypeDiscoveryContext Context { get; }

            public  IComplexOutputType Type { get; }

            public IComplexOutputTypeDefinition  Definition { get; }
        }

    }
}
