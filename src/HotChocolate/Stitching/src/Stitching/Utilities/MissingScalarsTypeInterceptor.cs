using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Utilities;

public class MissingScalarsTypeInterceptor : TypeInterceptor
{
    private bool _initialized;

    public override bool TriggerAggregations => true;

    public override IEnumerable<ITypeReference> RegisterMoreTypes(
        IReadOnlyCollection<ITypeDiscoveryContext> discoveryContexts)
    {
        if (!_initialized)
        {
            IDescriptorContext descriptorContext = discoveryContexts.First().DescriptorContext;
            Dictionary<NameString, ScalarType> registeredScalars = new();

            foreach (ScalarType scalarType in
                discoveryContexts.Select(t => t.Type).OfType<ScalarType>())
            {
                registeredScalars[scalarType.Name] = scalarType;
            }

            if (descriptorContext.ContextData.TryGetValue(
                HotChocolate.WellKnownContextData.SchemaDocuments,
                out var value) &&
                value is IReadOnlyList<DocumentNode> documents)
            {
                foreach (DocumentNode document in documents)
                {
                    foreach (ScalarTypeDefinitionNode scalarDefinition in
                        document.Definitions.OfType<ScalarTypeDefinitionNode>())
                    {
                        if (!registeredScalars.ContainsKey(scalarDefinition.Name.Value))
                        {
                            if (scalarDefinition.Name.Value.EqualsOrdinal("Upload"))
                            {
                                yield return descriptorContext.TypeInspector.GetTypeRef(
                                    typeof(UploadType));
                            }
                            else
                            {
                                yield return TypeReference.Create(
                                    new AnyType(
                                        scalarDefinition.Name.Value,
                                        scalarDefinition.Description?.Value));
                            }
                        }
                    }
                }
            }

            _initialized = true;
        }
    }
}
