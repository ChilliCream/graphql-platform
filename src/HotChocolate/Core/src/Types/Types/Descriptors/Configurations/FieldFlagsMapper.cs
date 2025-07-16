#nullable enable
using System.Runtime.CompilerServices;

namespace HotChocolate.Types.Descriptors.Configurations;

internal static class FieldFlagsMapper
{
    private const CoreFieldFlags PublicFieldMask =
        CoreFieldFlags.Deprecated
        | CoreFieldFlags.Introspection
        | CoreFieldFlags.SchemaIntrospectionField
        | CoreFieldFlags.TypeIntrospectionField
        | CoreFieldFlags.TypeNameIntrospectionField
        | CoreFieldFlags.FilterArgument
        | CoreFieldFlags.FilterOperationField
        | CoreFieldFlags.FilterExpensiveOperationField
        | CoreFieldFlags.SortArgument
        | CoreFieldFlags.SortOperationField
        | CoreFieldFlags.Connection
        | CoreFieldFlags.ConnectionEdgesField
        | CoreFieldFlags.ConnectionNodesField
        | CoreFieldFlags.ConnectionFirstArgument
        | CoreFieldFlags.ConnectionLastArgument
        | CoreFieldFlags.ConnectionAfterArgument
        | CoreFieldFlags.ConnectionBeforeArgument
        | CoreFieldFlags.GlobalIdNodeField
        | CoreFieldFlags.GlobalIdNodesField
        | CoreFieldFlags.CollectionSegment
        | CoreFieldFlags.CollectionSegmentItemsField
        | CoreFieldFlags.CollectionSegmentSkipArgument
        | CoreFieldFlags.CollectionSegmentTakeArgument
        | CoreFieldFlags.TotalCount;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FieldFlags MapToPublic(CoreFieldFlags coreFlags)
        => (FieldFlags)(coreFlags & PublicFieldMask);
}
