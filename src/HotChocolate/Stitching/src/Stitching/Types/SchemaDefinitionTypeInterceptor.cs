using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;

namespace HotChocolate.Stitching.Types
{
    internal class SchemaDefinitionTypeInterceptor : TypeInterceptor
    {
        private const string _schemaDefinition = "_schemaDefinition";
        private const string _configuration = "configuration";

        public override void OnBeforeCompleteType(
            ITypeCompletionContext completionContext,
            DefinitionBase? definition,
            IDictionary<string, object?> contextData)
        {
            // when we are visiting the query type we will add the schema definition field.
            if ((completionContext.IsQueryType ?? false) &&
                definition is ObjectTypeDefinition objectTypeDefinition)
            {
                ObjectFieldDefinition typeNameField = objectTypeDefinition.Fields.First(
                    t => t.Name.Equals(IntrospectionFields.TypeName) && t.IsIntrospectionField);
                var index = objectTypeDefinition.Fields.IndexOf(typeNameField) + 1;

                var descriptor = ObjectFieldDescriptor.New(
                    completionContext.DescriptorContext,
                    _schemaDefinition);

                descriptor
                    .Argument(_configuration, a => a.Type<NonNullType<StringType>>())
                    .Type<SchemaDefinitionType>()
                    .Resolve(ctx =>
                    {
                        string name = ctx.ArgumentValue<string>(_configuration);

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
