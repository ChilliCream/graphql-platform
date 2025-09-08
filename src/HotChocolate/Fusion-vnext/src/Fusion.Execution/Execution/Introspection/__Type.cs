using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __Type : ITypeResolverInterceptor
{
    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "kind":
                features.Set(new ResolveFieldValue(Kind));
                break;

            case "name":
                features.Set(new ResolveFieldValue(Name));
                break;

            case "description":
                features.Set(new ResolveFieldValue(Description));
                break;

            case "fields":
                features.Set(new ResolveFieldValue(Fields));
                break;

            case "interfaces":
                features.Set(new ResolveFieldValue(Interfaces));
                break;

            case "possibleTypes":
                features.Set(new ResolveFieldValue(PossibleTypes));
                break;

            case "enumValues":
                features.Set(new ResolveFieldValue(EnumValues));
                break;

            case "inputFields":
                features.Set(new ResolveFieldValue(InputFields));
                break;

            case "ofType":
                features.Set(new ResolveFieldValue(OfType));
                break;

            case "isOneOf":
                features.Set(new ResolveFieldValue(IsOneOf));
                break;

            case "specifiedBy":
                features.Set(new ResolveFieldValue(SpecifiedBy));
                break;
        }
    }

    public static void Kind(FieldContext context)
    {
        switch (context.Parent<IType>().Kind)
        {
            case TypeKind.Object:
                context.WriteValue(__TypeKind.Object);
                break;

            case TypeKind.Interface:
                context.WriteValue(__TypeKind.Interface);
                break;

            case TypeKind.Union:
                context.WriteValue(__TypeKind.Union);
                break;

            case TypeKind.InputObject:
                context.WriteValue(__TypeKind.InputObject);
                break;

            case TypeKind.Enum:
                context.WriteValue(__TypeKind.Enum);
                break;

            case TypeKind.Scalar:
                context.WriteValue(__TypeKind.Scalar);
                break;

            case TypeKind.List:
                context.WriteValue(__TypeKind.List);
                break;

            case TypeKind.NonNull:
                context.WriteValue(__TypeKind.NonNull);
                break;
        }
    }

    public static void Name(FieldContext context)
    {
        if (context.Parent<IType>() is ITypeDefinition typeDef)
        {
            context.WriteValue(typeDef.Name);
        }
    }

    public static void Description(FieldContext context)
    {
        if (context.Parent<IType>() is ITypeDefinition typeDef)
        {
            context.WriteValue(typeDef.Description);
        }
    }

    public static void Fields(FieldContext context)
    {
        var type = context.Parent<IType>();

        if (type is IComplexTypeDefinition ct)
        {
            var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
            var list = context.ResultPool.RentObjectListResult();
            context.FieldResult.SetNextValue(list);

            foreach (var field in ct.Fields)
            {
                if (field.IsIntrospectionField || (!includeDeprecated && field.IsDeprecated))
                {
                    continue;
                }

                context.AddRuntimeResult(field);
                list.SetNextValue(context.RentInitializedObjectResult());
            }
        }
    }

    public static void Interfaces(FieldContext context)
    {
        if (context.Parent<IType>() is IComplexTypeDefinition complexType)
        {
            var list = context.ResultPool.RentObjectListResult();
            context.FieldResult.SetNextValue(list);

            foreach (var type in complexType.Implements)
            {
                context.AddRuntimeResult(type);
                list.SetNextValue(context.RentInitializedObjectResult());
            }
        }
    }

    public static void PossibleTypes(FieldContext context)
    {
        if (context.Parent<IType>() is ITypeDefinition nt && nt.IsAbstractType())
        {
            var list = context.ResultPool.RentObjectListResult();
            context.FieldResult.SetNextValue(list);

            foreach (var type in context.Schema.GetPossibleTypes(nt))
            {
                context.AddRuntimeResult(type);
                list.SetNextValue(context.RentInitializedObjectResult());
            }
        }
    }

    public static void EnumValues(FieldContext context)
    {
        if (context.Parent<IType>() is IEnumTypeDefinition et)
        {
            var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
            var list = context.ResultPool.RentObjectListResult();
            context.FieldResult.SetNextValue(list);

            foreach (var value in et.Values)
            {
                if (!includeDeprecated && value.IsDeprecated)
                {
                    continue;
                }

                context.AddRuntimeResult(value);
                list.SetNextValue(context.RentInitializedObjectResult());
            }
        }
    }

    public static void InputFields(FieldContext context)
    {
        if (context.Parent<IType>() is IInputObjectTypeDefinition iot)
        {
            var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
            var list = context.ResultPool.RentObjectListResult();
            context.FieldResult.SetNextValue(list);

            foreach (var value in iot.Fields)
            {
                if (!includeDeprecated && value.IsDeprecated)
                {
                    continue;
                }

                context.AddRuntimeResult(value);
                list.SetNextValue(context.RentInitializedObjectResult());
            }
        }
    }

    public static void OfType(FieldContext context)
    {
        switch (context.Parent<IType>())
        {
            case ListType lt:
            {
                var obj = context.RentInitializedObjectResult();
                context.FieldResult.SetNextValue(obj);
                context.AddRuntimeResult(lt.ElementType);
                break;
            }

            case NonNullType nnt:
            {
                var obj = context.ResultPool.RentObjectListResult();
                context.FieldResult.SetNextValue(obj);
                context.AddRuntimeResult(nnt.NullableType);
                break;
            }
        }
    }

    public static void IsOneOf(FieldContext context)
    {
        if (context.Parent<IType>() is IInputObjectTypeDefinition iot)
        {
            context.WriteValue(iot.Directives.ContainsName(DirectiveNames.OneOf.Name));
        }
    }

    public static void SpecifiedBy(FieldContext context)
    {
        if (context.Parent<IType>() is IScalarTypeDefinition { SpecifiedBy: not null } scalar)
        {
            context.WriteValue(scalar.SpecifiedBy);
        }
    }
}
