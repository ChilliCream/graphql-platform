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
        private readonly Dictionary<NameString, ScalarInfo> _scalarInfos;
        private readonly List<Scalar> _scalars = new();

        public LeafTypeInterceptor(Dictionary<NameString, ScalarInfo> scalarInfos)
        {
            _scalarInfos = scalarInfos ?? throw new ArgumentNullException(nameof(scalarInfos));
        }

        public override void OnBeforeCompleteName(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            if (completionContext.Type is ScalarType scalarType)
            {
                _scalars.Add(new Scalar(scalarType, contextData));
            }
        }

        public override void OnAfterCompleteTypeNames()
        {
            foreach (Scalar scalar in _scalars)
            {
                if (_scalarInfos.TryGetValue(scalar.Type.Name, out ScalarInfo scalarInfo))
                {
                    scalar.ContextData[RuntimeType] = scalarInfo.RuntimeTypeType;
                    scalar.ContextData[SerializationType] = scalarInfo.SerializationType;
                }
                else
                {
                    scalar.ContextData[RuntimeType] = TypeNames.SystemString;
                    scalar.ContextData[SerializationType] = TypeNames.SystemString;
                }
            }
        }

        private readonly struct Scalar
        {
            public Scalar(ScalarType type, IDictionary<string, object?> contextData)
            {
                Type = type;
                ContextData = contextData;
            }

            public ScalarType Type { get; }

            public IDictionary<string, object?> ContextData { get; }
        }
    }
}
