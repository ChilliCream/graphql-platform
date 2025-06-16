using HotChocolate.Configuration;
using HotChocolate.Features;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Configurations;
using HotChocolate.Types.Mutable;
using EnumTypeDefinition = HotChocolate.Types.Descriptors.Configurations.EnumTypeDefinition;
using InterfaceTypeDefinition = HotChocolate.Types.Descriptors.Configurations.InterfaceTypeDefinition;
using ObjectTypeDefinition = HotChocolate.Types.Descriptors.Configurations.ObjectTypeDefinition;
using UnionTypeDefinition = HotChocolate.Types.Descriptors.Configurations.UnionTypeDefinition;

namespace HotChocolate.OpenApi.TypeInterceptors;

/// <summary>
/// Copies OpenAPI-related context data from the Mutable schema to the type definitions.
/// </summary>
public sealed class ContextDataTypeInterceptor(MutableSchemaDefinition schema) : TypeInterceptor
{
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase definition)
    {
        switch (completionContext.Type)
        {
            case EnumType type when definition is EnumTypeDefinition typeDef:
                CopyContextData(new SchemaCoordinate(type.Name), typeDef);
                break;

            case InterfaceType type when definition is InterfaceTypeDefinition typeDef:
                CopyContextData(new SchemaCoordinate(type.Name), typeDef);
                break;

            case ObjectType type when definition is ObjectTypeDefinition typeDef:
                CopyContextData(new SchemaCoordinate(type.Name), typeDef);
                break;

            case UnionType type when definition is UnionTypeDefinition typeDef:
                CopyContextData(new SchemaCoordinate(type.Name), typeDef);
                break;
        }
    }

    private void CopyContextData(SchemaCoordinate coordinate, IDefinition memberDef)
    {
        ITypeSystemMember? member = null;

        switch (coordinate.Name)
        {
            case OperationTypeNames.Mutation:
                if (coordinate.MemberName is null)
                {
                    member = schema.MutationType;
                }
                else
                {
                    if (schema.MutationType!.Fields.TryGetField(coordinate.MemberName, out var field))
                    {
                        member = field;
                    }
                }
                break;

            case OperationTypeNames.Query:
                if (coordinate.MemberName is null)
                {
                    member = schema.QueryType;
                }
                else
                {
                    if (schema.QueryType!.Fields.TryGetField(coordinate.MemberName, out var field))
                    {
                        member = field;
                    }
                }
                break;

            default:
            {
                if (!schema.TryGetMember(coordinate, out member))
                {
                    return;
                }
                break;
            }
        }

        if (member is IFeatureProvider featureProvider)
        {
            var typeMetadata = featureProvider.Features.Get<OpenApiTypeMetadata>();
            if(typeMetadata?.TypeMap is not null)
            {
                memberDef.ContextData[WellKnownContextData.OpenApiTypeMap] = typeMetadata.TypeMap;
            }

            var fieldMetadata = featureProvider.Features.Get<OpenApiFieldMetadata>();
            if (fieldMetadata is not null)
            {
                if (fieldMetadata.InputFieldName is not null)
                {
                    memberDef.ContextData[WellKnownContextData.OpenApiInputFieldName] = fieldMetadata.InputFieldName;
                }

                if (fieldMetadata.IsErrorsField)
                {
                    memberDef.ContextData[WellKnownContextData.OpenApiIsErrorsField] = true;
                }

                if (fieldMetadata.PropertyName is not null)
                {
                    memberDef.ContextData[WellKnownContextData.OpenApiPropertyName] = fieldMetadata.PropertyName;
                }

                if (fieldMetadata.Resolver is not null)
                {
                    memberDef.ContextData[WellKnownContextData.OpenApiResolver] = fieldMetadata.Resolver;
                }

                if (fieldMetadata.UseParentResult)
                {
                    memberDef.ContextData[WellKnownContextData.OpenApiUseParentResult] = true;
                }
            }
        }

        switch (memberDef)
        {
            case InterfaceTypeDefinition i:
                foreach (var field in i.Fields)
                {
                    CopyContextData(new SchemaCoordinate(i.Name, field.Name), field);
                }
                break;

            case ObjectTypeDefinition o:
                foreach (var field in o.Fields)
                {
                    CopyContextData(new SchemaCoordinate(o.Name, field.Name), field);
                }
                break;
        }
    }
}
