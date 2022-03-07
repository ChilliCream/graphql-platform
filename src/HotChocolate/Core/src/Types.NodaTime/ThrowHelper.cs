using HotChocolate.Types.NodaTime.Properties;

namespace HotChocolate.Types.NodaTime;

internal static class ThrowHelper
{
    public static SchemaException PatternCannotBeEmpty(ITypeSystemObject type) =>
        new(SchemaErrorBuilder
            .New()
            .SetTypeSystemObject(type)
            .SetMessage(NodaTimeResources.NodaTime_NoPatternProvided, type.Name)
            .Build());
}
