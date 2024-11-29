using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

internal static class ErrorHelper
{
    public static ISchemaError CacheControlInheritMaxAgeOnType(ITypeSystemObject type)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlInheritMaxAgeOnType,
                type.Name)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError CacheControlInheritMaxAgeOnQueryTypeField(
        ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlInheritMaxAgeOnQueryTypeField,
                field.Coordinate.ToString(),
                type.Name)
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError CacheControlOnInterfaceField(
        ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlOnInterfaceField,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError CacheControlNegativeMaxAge(
        ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlNegativeMaxAge,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError CacheControlNegativeSharedMaxAge(
        ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlNegativeSharedMaxAge,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError CacheControlBothMaxAgeAndInheritMaxAge(
        ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlBothMaxAgeAndInheritMaxAge,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError CacheControlBothSharedMaxAgeAndInheritMaxAge(
        ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(
                ErrorHelper_CacheControlBothSharedMaxAgeAndInheritMaxAge,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .Build();
}
