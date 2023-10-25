using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;
using HotChocolate.Types.Introspection;
using static HotChocolate.Stitching.SchemaDefinitions.SchemaDefinitionFieldNames;

namespace HotChocolate.Stitching.SchemaDefinitions;

internal sealed class SchemaDefinitionTypeInterceptor : TypeInterceptor
{
    private readonly bool _publishOnSchema;
    private ITypeCompletionContext _queryContext = default!;

    public SchemaDefinitionTypeInterceptor(bool publishOnSchema)
    {
        _publishOnSchema = publishOnSchema;
    }

    internal override void OnAfterResolveRootType(
        ITypeCompletionContext completionContext,
        ObjectTypeDefinition definition,
        OperationType operationType)
    {
        if (operationType is OperationType.Query)
        {
            _queryContext = completionContext;
        }
    }

    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        // when we are visiting the query type we will add the schema definition field.
        if (_publishOnSchema &&
            ReferenceEquals(completionContext, _queryContext) &&
            definition is ObjectTypeDefinition objectTypeDefinition &&
            !objectTypeDefinition.Fields.Any(t => t.Name.Equals(SchemaDefinitionField)))
        {
            var typeNameField = objectTypeDefinition.Fields.First(
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
                    var name = ctx.ArgumentValue<string>(ConfigurationArgument);

                    return ctx.Schema.ContextData
                        .GetSchemaDefinitions()
                        .FirstOrDefault(t => t.Name.Equals(name));
                });

            objectTypeDefinition.Fields.Insert(index, descriptor.CreateDefinition());
        }

        // when we visit the schema definition we will copy over the schema definition list
        // that sits on the schema creation context.
        else if (definition is SchemaTypeDefinition schemaTypeDef &&
            completionContext.ContextData.TryGetValue(
                WellKnownContextData.SchemaDefinitions,
                out var schemaDefinitions))
        {
            schemaTypeDef.ContextData[WellKnownContextData.SchemaDefinitions] = schemaDefinitions;
        }
    }
}
