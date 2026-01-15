namespace HotChocolate.Types.Spatial;

internal static class WellKnownFields
{
    public const string TypeFieldName = "type";
    public static ReadOnlySpan<byte> TypeFieldNameBytes => "type"u8;

    public const string CoordinatesFieldName = "coordinates";
    public static ReadOnlySpan<byte> CoordinatesFieldNameBytes => "coordinates"u8;

    public const string CrsFieldName = "crs";
    public static ReadOnlySpan<byte> CrsFieldNameBytes => "crs"u8;

    public const string BboxFieldName = "bbox";
    public static ReadOnlySpan<byte> BboxFieldNameBytes => "bbox"u8;
}
