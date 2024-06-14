using HotChocolate.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.OpenApi.TypeInterceptors;

/// <summary>
/// Copies OpenAPI-related context data from the skimmed schema to the type definitions.
/// </summary>
public sealed class ContextDataTypeInterceptor(Skimmed.SchemaDefinition schema) : TypeInterceptor
{
    private static readonly List<string> CopyKeys =
    [
        WellKnownContextData.OpenApiInputFieldName,
        WellKnownContextData.OpenApiIsErrorsField,
        WellKnownContextData.OpenApiPropertyName,
        WellKnownContextData.OpenApiResolver,
        WellKnownContextData.OpenApiTypeMap,
        WellKnownContextData.OpenApiUseParentResult,
    ];

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

    private void CopyContextData(SchemaCoordinate coordinate, IDefinition typeDef)
    {
        Skimmed.ITypeSystemMemberDefinition? type = null;

        switch (coordinate.Name)
        {
            case OperationTypeNames.Mutation:
                if (coordinate.MemberName is null)
                {
                    type = schema.MutationType;
                }
                else
                {
                    if (schema.MutationType!.Fields.TryGetField(
                            coordinate.MemberName, out var field))
                    {
                        type = field;
                    }
                }

                break;

            case OperationTypeNames.Query:
                if (coordinate.MemberName is null)
                {
                    type = schema.QueryType;
                }
                else
                {
                    if (schema.QueryType!.Fields.TryGetField(
                            coordinate.MemberName, out var field))
                    {
                        type = field;
                    }
                }

                break;

            default:
            {
                if (!schema.TryGetMember(coordinate, out type))
                {
                    return;
                }

                break;
            }
        }

        if (type is IHasContextData typeWithContextData)
        {
            foreach (var key in CopyKeys)
            {
                if (typeWithContextData.ContextData.TryGetValue(key, out var value))
                {
                    typeDef.ContextData[key] = value;
                }
            }
        }

        switch (typeDef)
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
