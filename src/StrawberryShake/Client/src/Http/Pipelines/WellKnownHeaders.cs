namespace StrawberryShake.Http.Pipelines
{
    internal static class WellKnownHeaders
    {
        public static HeaderInfo ContentTypeJson { get; } =
            new HeaderInfo("Content-Type", "application/json");
    }
}
