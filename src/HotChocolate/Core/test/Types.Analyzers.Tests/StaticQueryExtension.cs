using HotChocolate.Language;

namespace HotChocolate.Types;

[ExtendObjectType(OperationType.Query)]
public static class StaticQueryExtension
{
    public static string StaticField() => "foo";
}