namespace HotChocolate.Execution.Instrumentation
{
    internal static class DiagnosticNames
    {
        private const string _startPrefix = ".Start";
        private const string _stopPrefix = ".Stop";

        public const string Listener = "HotChocolate.Execution";

        public const string Query = Listener + ".Query";
        public const string QueryError = Query + ".Error";
        public const string StartQuery = Query + _startPrefix;
        public const string StopQuery = Query + _stopPrefix;

        public const string Parsing = Listener + ".Parsing";
        public const string StartParsing = Parsing + _startPrefix;
        public const string StopParsing = Parsing + _stopPrefix;

        public const string Validation = Listener + ".Validation";
        public const string ValidationError = Validation + ".Error";
        public const string StartValidation = Validation + _startPrefix;
        public const string StopValidation = Validation + _stopPrefix;

        public const string Resolver = Listener + ".Resolver";
        public const string ResolverError = Resolver + ".Error";
        public const string StartResolver = Resolver + _startPrefix;
        public const string StopResolver = Resolver + _stopPrefix;
    }
}
