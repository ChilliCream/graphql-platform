using System.Collections.Generic;
using HotChocolate.Types.Descriptors;
using static HotChocolate.Configuration.Bindings.SchemaFirstContextData;

#nullable enable

namespace HotChocolate.Configuration.Bindings
{
    internal class SchemaFirstSchemaInterceptor : SchemaInterceptor
    {
        public override void OnBeforeCreate(
            IDescriptorContext context,
            ISchemaBuilder schemaBuilder)
        {
            if (context.ContextData.TryGetValue(EnumTypeConfigs, out object? o) &&
                o is List<EnumTypeBindingConfiguration> configs)
            {
                var bindings = new Dictionary<NameString, EnumTypeBindingDefinition>();

                foreach (EnumTypeBindingConfiguration config in configs)
                {
                    var descriptor = new EnumTypeBindingDescriptor(context, config.RuntimeType);
                    config.Configure(descriptor);
                    bindings[descriptor.Definition.TypeName] = descriptor.Definition;
                }

                context.ContextData.Add(EnumTypes, bindings);
            }
        }
    }
}
