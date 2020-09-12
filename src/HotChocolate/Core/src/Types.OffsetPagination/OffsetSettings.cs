namespace HotChocolate.Types.Pagination
{
    public struct OffsetSettings
    {
        public int? DefaultPageSize { get; set; }

        public int? MaxPageSize { get; set; }

        public bool? WithTotalCount { get; set; }

        internal static string GetKey() => typeof(OffsetSettings).FullName;
    }
}
