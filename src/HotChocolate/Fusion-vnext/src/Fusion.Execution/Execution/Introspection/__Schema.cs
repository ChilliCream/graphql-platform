#pragma warning disable IDE1006 // Naming Styles
using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Types.Introspection;

// ReSharper disable once InconsistentNaming
internal static class __Schema
{
    public static void Description(FieldContext context)
        => context.WriteValue(context.Schema.Description);

    public static void Types(FieldContext context)
    {
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var type in context.Schema.Types)
        {
            context.AddRuntimeResult(type);
            list.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void QueryType(FieldContext context)
    {
        context.AddRuntimeResult(context.Schema.QueryType);
        context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
    }

    public static void MutationType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void SubscriptionType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void Directives(FieldContext context)
    {
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var directiveDefinition in context.Schema.DirectiveDefinitions)
        {
            context.AddRuntimeResult(directiveDefinition);
            list.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }
}

// ReSharper disable once InconsistentNaming
internal static class __Type
{
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

    public static object? Fields(FieldContext context)
    {
        var type = context.Parent<IType>();

        if (type is IComplexTypeDefinition ct)
        {
            var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated");

            return !includeDeprecated.Value
                ? ct.Fields.Where(t => !t.IsIntrospectionField && !t.IsDeprecated)
                : ct.Fields.Where(t => !t.IsIntrospectionField);
        }

        return null;
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
                list.SetNextValue(context.ResultPool.RentObjectResult());
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
                list.SetNextValue(context.ResultPool.RentObjectResult());
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
                list.SetNextValue(context.ResultPool.RentObjectResult());
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
                list.SetNextValue(context.ResultPool.RentObjectResult());
            }
        }
    }

    public static void OfType(FieldContext context)
    {
        switch (context.Parent<IType>())
        {
            case ListType lt:
            {
                var obj = context.ResultPool.RentObjectResult();
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
        if (context.Parent<IType>() is IScalarTypeDefinition scalar )
        {
            // TODO : Implement
            // context.WriteValue(scalar.SpecifiedBy?.ToString());

        }
    }
}

// ReSharper disable once InconsistentNaming
internal static class __TypeKind
{
    public static ReadOnlySpan<byte> Scalar => "SCALAR"u8;
    public static ReadOnlySpan<byte> Object => "OBJECT"u8;
    public static ReadOnlySpan<byte> Interface => "INTERFACE"u8;
    public static ReadOnlySpan<byte> Union => "UNION"u8;
    public static ReadOnlySpan<byte> Enum => "ENUM"u8;
    public static ReadOnlySpan<byte> InputObject => "INPUT_OBJECT"u8;
    public static ReadOnlySpan<byte> List => "LIST"u8;
    public static ReadOnlySpan<byte> NonNull => "NON_NULL"u8;
}

file static class MemHelper
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    public static void WriteValue(this FieldContext context, string? s)
    {
        if (s is null)
        {
            return;
        }

        var start = context.Memory.Length;
        var expectedSize = s_utf8.GetByteCount(s);
        var span = context.Memory.GetSpan(expectedSize + 1);
        span[0] = RawFieldValueType.String;
        var written = s_utf8.GetBytes(s, span[1..]);
        context.Memory.Advance(written + 1);
        var segment = context.Memory.GetWrittenMemorySegment(start, written + 1);
        context.FieldResult.SetNextValue(segment);
    }

    public static void WriteValue(this FieldContext context, bool b)
    {
        var start = context.Memory.Length;
        const int length = 2;
        var span = context.Memory.GetSpan(length);
        span[0] = RawFieldValueType.Boolean;
        span[1] = b ? (byte)1 : (byte)0;
        context.Memory.Advance(length);
        var segment = context.Memory.GetWrittenMemorySegment(start, length);
        context.FieldResult.SetNextValue(segment);
    }

    public static void WriteValue(this FieldContext context, ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            return;
        }

        var start = context.Memory.Length;
        var length = value.Length + 1;
        var span = context.Memory.GetSpan(length);
        span[0] = RawFieldValueType.String;
        value.CopyTo(span[1..]);
        context.Memory.Advance(length);
        var segment = context.Memory.GetWrittenMemorySegment(start, length);
        context.FieldResult.SetNextValue(segment);
    }
}

#pragma warning restore IDE1006 // Naming Styles
