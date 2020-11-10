using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Configuration.Bindings.SchemaFirstContextData;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class SchemaFirstTypeInterceptor : TypeInterceptor
    {
        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?>? contextData)
        {
            if (discoveryContext.Type is EnumType &&
                definition is EnumTypeDefinition enumTypeDef &&
                discoveryContext.ContextData.TryGetValue(EnumTypes, out object o) &&
                o is IDictionary<NameString, EnumTypeBindingDefinition> bindings &&
                bindings.TryGetValue(enumTypeDef.Name, out EnumTypeBindingDefinition binding))
            {
                enumTypeDef.RuntimeType = binding.RuntimeType;
                IDictionary<NameString, object?> values =
                    binding.Values.ToDictionary(v => v.Name, v => v.Value);

                foreach (EnumValueDefinition value in enumTypeDef.Values)
                {
                    if (values.TryGetValue(value.Name, out object? runtimeValue))
                    {
                        value.Value = runtimeValue;
                    }
                }
            }
        }
    }
}
