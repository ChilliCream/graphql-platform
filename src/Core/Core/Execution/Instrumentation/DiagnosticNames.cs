namespace HotChocolate.Execution.Instrumentation
{
    internal static class DiagnosticNames
    {
        public const string Listener = "HotChocolate.Execution";

        public const string Query = "Query";
        public const string QueryError = "Query.Error";
        public const string StartQuery = Query + ".Start";
        public const string StopQuery = Query + ".Stop";

        public const string Parsing = "Parsing";
        public const string StartParsing = Parsing + ".Start";
        public const string StopParsing = Parsing + ".Stop";

        public const string Validation = "Validation";
        public const string ValidationError = "Validation.Error";
        public const string StartValidation = Validation + ".Start";
        public const string StopValidation = Validation + ".Stop";

        public const string Resolver = "Resolver";
        public const string ResolverError = "Resolver.Error";
        public const string StartResolver = Resolver + ".Start";
        public const string StopResolver = Resolver + ".Stop";
    }
}
