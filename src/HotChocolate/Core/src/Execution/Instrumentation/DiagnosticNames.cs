namespace HotChocolate.Execution.Instrumentation
{
    public static class DiagnosticNames
    {
        private const string _startPostfix = ".Start";
        private const string _stopPostfix = ".Stop";
        private const string _errorPostfix = ".Error";

        public const string Listener = "HotChocolate.Execution";

        public const string Query = Listener + ".Query";
        public const string QueryError = Query + _errorPostfix;
        public const string StartQuery = Query + _startPostfix;
        public const string StopQuery = Query + _stopPostfix;

        public const string Parsing = Listener + ".Parsing";
        public const string StartParsing = Parsing + _startPostfix;
        public const string StopParsing = Parsing + _stopPostfix;

        public const string Validation = Listener + ".Validation";
        public const string ValidationError = Validation + _errorPostfix;
        public const string StartValidation = Validation + _startPostfix;
        public const string StopValidation = Validation + _stopPostfix;

        public const string Operation = Listener + ".Operation";
        public const string StartOperation = Operation + _startPostfix;
        public const string StopOperation = Operation + _stopPostfix;

        public const string Resolver = Listener + ".Resolver";
        public const string ResolverError = Resolver + _errorPostfix;
        public const string StartResolver = Resolver + _startPostfix;
        public const string StopResolver = Resolver + _stopPostfix;
    }
}
