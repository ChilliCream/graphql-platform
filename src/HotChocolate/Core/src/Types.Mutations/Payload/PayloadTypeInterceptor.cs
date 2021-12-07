using System.Collections.Generic;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types;

internal class PayloadTypeInterceptor : TypeInterceptor
{
    public override void OnBeforeRegisterDependencies(
        ITypeDiscoveryContext discoveryContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is not ObjectTypeDefinition def)
        {
            return;
        }

        foreach (ObjectFieldDefinition? field in def.Fields)
        {
            if (!(field.ContextData.TryGetValue(PayloadContextData.Payload, out var contextObj) &&
                    contextObj is PayloadContextData context &&
                    field.Type is { }))
            {
                continue;
            }

            ITypeReference? fieldType = field.Type;

            FieldMiddlewareDefinition middlewareDefinition =
                new(FieldClassMiddlewareFactory.Create<PayloadMiddleware>(),
                    false,
                    PayloadMiddleware.MiddlewareIdentifier);

            field.MiddlewareDefinitions.Insert(0, middlewareDefinition);

            NameString typeName =
                context.TypeName ?? field.Name.ToTypeName(suffix: "Payload");

            field.Type = new DependantFactoryTypeReference(
                typeName,
                fieldType,
                CreateType,
                TypeContext.Output);

            TypeSystemObjectBase CreateType(IDescriptorContext descriptorContext) =>
                new ObjectType<Payload>(descriptor =>
                {
                    descriptor.Name(typeName);

                    const string placeholder = "payload";

                    IObjectFieldDescriptor resultField =
                        descriptor.Field(x => x.Result).Name(placeholder);

                    resultField.Extend().OnBeforeCreate(x => x.Type = fieldType);
                    resultField.Extend().OnBeforeNaming(OnBeforeNaming);

                    void OnBeforeNaming(
                        ITypeCompletionContext ctx,
                        ObjectFieldDefinition fieldDefinition)
                    {
                        if (context.FieldName is not null)
                        {
                            fieldDefinition.Name = context.FieldName;
                            return;
                        }

                        IType type = ctx.GetType<IType>(fieldType);
                        fieldDefinition.Name = type.NamedType().Name.Value.ToFieldName();
                    }
                });
        }
    }
}
