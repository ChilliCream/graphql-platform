using System.Collections.Frozen;
using HotChocolate.Language;
using HotChocolate.Types;
using static HotChocolate.ModelContextProtocol.Properties.ModelContextProtocolResources;

namespace HotChocolate.ModelContextProtocol.Extensions;

internal static class TypeNodeExtensions
{
    public static IType ToType(this ITypeNode typeNode, ISchemaDefinition schema)
    {
        var typeName = typeNode.NamedType().Name.Value;

        if (schema.Types.TryGetType(typeName, out var typeDefinition))
        {
            return typeNode.RewriteToType(typeDefinition);
        }

        if (s_typeMap.TryGetValue(typeName, out var type))
        {
            return typeNode.RewriteToType(type.AsTypeDefinition());
        }

        throw new NotSupportedException(
            string.Format(TypeNodeExtensions_UnableToFindGraphQLTypeInSchemaOrTypeMap, typeName));
    }

    private static readonly FrozenDictionary<string, IType> s_typeMap =
        new Dictionary<string, IType>
        {
            { ScalarNames.Any, new AnyType() },
            { ScalarNames.Boolean, new BooleanType() },
            { ScalarNames.Byte, new ByteType() },
            { ScalarNames.ByteArray, new ByteArrayType() },
            { ScalarNames.Date, new DateType() },
            { ScalarNames.DateTime, new DateTimeType() },
            { ScalarNames.Decimal, new DecimalType() },
            { ScalarNames.Float, new FloatType() },
            { ScalarNames.ID, new IdType() },
            { ScalarNames.Int, new IntType() },
            { ScalarNames.JSON, new JsonType() },
            { ScalarNames.LocalDate, new LocalDateType() },
            { ScalarNames.LocalDateTime, new LocalDateTimeType() },
            { ScalarNames.LocalTime, new LocalTimeType() },
            { ScalarNames.Long, new LongType() },
            { ScalarNames.Short, new ShortType() },
            { ScalarNames.String, new StringType() },
            { ScalarNames.TimeSpan, new TimeSpanType() },
            { ScalarNames.URL, new UrlType() },
            { ScalarNames.UUID, new UuidType() }
        }.ToFrozenDictionary();
}
