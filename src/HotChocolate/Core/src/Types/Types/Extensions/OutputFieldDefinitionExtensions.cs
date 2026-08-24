using HotChocolate.Types.Descriptors.Configurations;

namespace HotChocolate.Types;

internal static class OutputFieldDefinitionExtensions
{
    /// <summary>
    /// Checks if all of the specified <paramref name="flags"/> are set on the field.
    /// </summary>
    public static bool HasCoreFieldFlags(this IOutputFieldDefinition field, CoreFieldFlags flags)
        => field is FieldBase fieldBase && (fieldBase.Flags & flags) == flags;
}
