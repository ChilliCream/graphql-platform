using System;
using System.Collections.Generic;
using HotChocolate;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using StrawberryShake.CodeGeneration.Analyzers;
using static StrawberryShake.CodeGeneration.Analyzers.WellKnownContextData;

namespace StrawberryShake.CodeGeneration.Utilities
{
    public class LeafTypeInterceptor : TypeInterceptor
    {
        private readonly Dictionary<NameString, LeafTypeInfo> _scalarInfos;
        private readonly List<LeafType> _leafTypes = new();

        public LeafTypeInterceptor(Dictionary<NameString, LeafTypeInfo> scalarInfos)
        {
            _scalarInfos = scalarInfos ?? throw new ArgumentNullException(nameof(scalarInfos));
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ILeafType leafType)
            {
                _leafTypes.Add(new LeafType(leafType, contextData));
            }
        }

        public override void OnAfterCompleteTypeNames()
        {
            foreach (LeafType leafType in _leafTypes)
            {
                if (_scalarInfos.TryGetValue(leafType.Type.Name, out LeafTypeInfo scalarInfo))
                {
                    if (leafType.Type is ScalarType)
                    {
                        leafType.ContextData[RuntimeType] = scalarInfo.RuntimeType;
                    }
                    leafType.ContextData[SerializationType] = scalarInfo.SerializationType;
                }
                else
                {
                    if (leafType.Type is ScalarType)
                    {
                        leafType.ContextData[RuntimeType] = TypeNames.String;
                    }
                    leafType.ContextData[SerializationType] = TypeNames.String;
                }
            }
        }

        private readonly struct LeafType
        {
            public LeafType(ILeafType type, IDictionary<string, object?> contextData)
            {
                Type = type;
                ContextData = contextData;
            }

            public ILeafType Type { get; }

            public IDictionary<string, object?> ContextData { get; }
        }
    }
}
