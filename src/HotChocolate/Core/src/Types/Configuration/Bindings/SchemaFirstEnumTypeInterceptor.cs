using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class SchemaFirstEnumTypeInterceptor : TypeInterceptor
    {
        public const string Key = "HotChocolate.Configuration.Bindings.SDL.EnumTypes";

        public override void OnAfterInitialize(
            ITypeDiscoveryContext discoveryContext,
            DefinitionBase? definition,
            IDictionary<string, object?>? contextData)
        {
            if (discoveryContext.Type is EnumType &&
                definition is EnumTypeDefinition enumTypeDef &&
                discoveryContext.ContextData.TryGetValue(Key, out object? o) &&
                o is Dictionary<NameString, EnumTypeBindingDefinition> bindings &&
                bindings.TryGetValue(enumTypeDef.Name, out EnumTypeBindingDefinition? binding))
            {
                enumTypeDef.RuntimeType = binding.RuntimeType;
                Dictionary<NameString, object?> values =
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
