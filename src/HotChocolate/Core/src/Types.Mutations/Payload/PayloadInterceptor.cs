using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Resolvers;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Definitions;

#nullable enable

namespace HotChocolate.Types.Payload;

internal class PayloadInterceptor : TypeInterceptor
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

        foreach (var field in def.Fields)
        {
            if (field.ContextData.TryGetValue(PayloadContextData.Payload, out var contextObj) &&
                contextObj is PayloadContextData context &&
                field.Type is { })
            {
                string name =
                    context.FieldName ??
                    field.ResultType?.Name.ToFieldName() ??
                    "payload";

                ITypeReference? fieldType = field.Type;

                FieldMiddlewareDefinition middlewareDefinition =
                    new(FieldClassMiddlewareFactory.Create<PayloadMiddleware>(),
                        false,
                        PayloadMiddleware.MiddlewareIdentifier);

                field.MiddlewareDefinitions.Insert(0, middlewareDefinition);

                NameString typeName = context.TypeName ?? field.Name.ToTypeName(suffix: "Payload");

                field.Type = new DependantFactoryTypeReference(
                    typeName,
                    fieldType,
                    CreateType,
                    TypeContext.Output);

                TypeSystemObjectBase CreateType(IDescriptorContext descriptorContext) =>
                    new ObjectType<Payload>(descriptor =>
                    {
                        descriptor.Name(typeName);
                        descriptor
                            .Field(x => x.Result)
                            .Name(name)
                            .Extend()
                            .Definition.Type = fieldType;
                    });
            }
        }
    }
}
