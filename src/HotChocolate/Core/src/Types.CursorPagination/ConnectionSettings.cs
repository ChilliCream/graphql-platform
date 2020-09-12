namespace HotChocolate.Types.Relay
{
    public struct ConnectionSettings
    {
        public int? DefaultPageSize { get; set; }

        public int? MaxPageSize { get; set; }

        public bool? WithTotalCount { get; set; }

        internal static string GetKey() => typeof(ConnectionSettings).FullName;
    }
}
