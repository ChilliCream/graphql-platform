using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using static HotChocolate.Stitching.SchemaDefinitions.SchemaDefinitionFieldNames;

namespace HotChocolate.Stitching.SchemaDefinitions
{
    internal class SchemaDefinitionTypeInterceptor : TypeInterceptor
    {
        private readonly bool _publishOnSchema;

        public SchemaDefinitionTypeInterceptor(bool publishOnSchema)
        {
            _publishOnSchema = publishOnSchema;
        }

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            // when we are visiting the query type we will add the schema definition field.
            if (_publishOnSchema &&
                (completionContext.IsQueryType ?? false) &&
                definition is ObjectTypeDefinition objectTypeDefinition &&
                !objectTypeDefinition.Fields.Any(t => t.Name.Equals(SchemaDefinitionField)))
            {
                ObjectFieldDefinition typeNameField = objectTypeDefinition.Fields.First(
                    t => t.Name.Equals(IntrospectionFields.TypeName) && t.IsIntrospectionField);
                var index = objectTypeDefinition.Fields.IndexOf(typeNameField) + 1;

                var descriptor = ObjectFieldDescriptor.New(
                    completionContext.DescriptorContext,
                    SchemaDefinitionField);

                descriptor
                    .Argument(ConfigurationArgument, a => a.Type<NonNullType<StringType>>())
                    .Type<SchemaDefinitionType>()
                    .Resolve(ctx =>
                    {
                        string name = ctx.ArgumentValue<string>(ConfigurationArgument);

                        return ctx.Schema.ContextData
                            .GetSchemaDefinitions()
                            .FirstOrDefault(t => t.Name.Equals(name));
                    });

                objectTypeDefinition.Fields.Insert(index, descriptor.CreateDefinition());
            }

            // when we visit the schema definition we will copy over the schema definition list
            // that sits on the schema creation context.
            else if (definition is SchemaTypeDefinition &&
                completionContext.ContextData.TryGetValue(
                    WellKnownContextData.SchemaDefinitions,
                    out object? schemaDefinitions))
            {
                contextData[WellKnownContextData.SchemaDefinitions] = schemaDefinitions;
            }
        }
    }
}
