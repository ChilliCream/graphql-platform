using HotChocolate.Types;
using static HotChocolate.Caching.Properties.CacheControlResources;

namespace HotChocolate.Caching;

internal static class ErrorHelper
{
    public static ISchemaError InheritMaxAgeCanNotBeOnType(ITypeSystemObject type)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_InheritMaxAgeCanNotBeOnType,
                type.Name.ToString())
            .SetTypeSystemObject(type)
            .Build();

    public static ISchemaError MaxAgeValueCanNotBeNegative(ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_MaxAgeValueCanNotBeNegative,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .AddSyntaxNode(field.SyntaxNode)
            .Build();

    public static ISchemaError BothInheritMaxAgeAndMaxAgeSpecified(ITypeSystemObject type,
        IField field)
        => SchemaErrorBuilder.New()
            .SetMessage(ErrorHelper_BothInheritMaxAgeAndMaxAgeSpecified,
                field.Coordinate.ToString())
            .SetTypeSystemObject(type)
            .AddSyntaxNode(field.SyntaxNode)
            .Build();
}
