using HotChocolate.Configuration;
using HotChocolate.Types.Descriptors.Definitions;
using static HotChocolate.Types.Introspection.IntrospectionFields;

namespace HotChocolate.Types.Introspection;

internal sealed class IntrospectionTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        if (definition is ObjectTypeDefinition objectTypeDefinition)
        {
            var position = 0;
            var context = completionContext.DescriptorContext;

            if (completionContext.IsQueryType ?? false)
            {
                objectTypeDefinition.Fields.Insert(position++, CreateSchemaField(context));
                objectTypeDefinition.Fields.Insert(position++, CreateTypeField(context));
            }

            objectTypeDefinition.Fields.Insert(position, CreateTypeNameField(context));
        }
    }
}
