namespace HotChocolate.Execution.Instrumentation
{
    internal static class DiagnosticNames
    {
        private const string _startPrefix = ".Start";
        private const string _stopPrefix = ".Stop";

        public const string Listener = "HotChocolate.Execution";

        public const string Query = "Query";
        public const string QueryError = "Query.Error";
        public const string StartQuery = Query + _startPrefix;
        public const string StopQuery = Query + _stopPrefix;

        public const string Parsing = "Parsing";
        public const string StartParsing = Parsing + _startPrefix;
        public const string StopParsing = Parsing + _stopPrefix;

        public const string Validation = "Validation";
        public const string ValidationError = "Validation.Error";
        public const string StartValidation = Validation + _startPrefix;
        public const string StopValidation = Validation + _stopPrefix;

        public const string Resolver = "Resolver";
        public const string ResolverError = "Resolver.Error";
        public const string StartResolver = Resolver + _startPrefix;
        public const string StopResolver = Resolver + _stopPrefix;
    }
}
