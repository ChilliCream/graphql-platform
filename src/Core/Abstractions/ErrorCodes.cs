namespace HotChocolate
{
    public static class ErrorCodes
    {
        public static class Authentication
        {
            public const string NotAuthorized = "AUTH_NOT_AUTHORIZED";
            public const string NotAuthenticated = "AUTH_NOT_AUTHENTICATED";
            public const string NoDefaultPolicy = "AUTH_NO_DEFAULT_POLICY";
            public const string PolicyNotFound = "AUTH_POLICY_NOT_FOUND";
        }

        internal static class Execution
        {
            public const string CannotSerialize = "EXEC_BATCH_VAR_SERIALIZE";
            public const string AutoMapVarError = "EXEC_BATCH_AUTO_MAP_VAR_TYPE";
            public const string Incomplete = "EXEC_MIDDLEWARE_INCOMPLETE";
            public const string Timeout = "EXEC_TIMEOUT";
            public const string QueryNotFound = "PERSISTED_QUERY_NOT_FOUND";
            public const string NonNullViolation = "EXEC_NON_NULL_VIOLATION";
        }

        internal static class Schema
        {
            public const string NoEnumValues = "TS_NO_ENUM_VALUES";
            public const string MissingType = "TS_MISSING_TYPE";
            public const string NoResolver = "TS_NO_FIELD_RESOLVER";
            public const string UnresolvedTypes = "TS_UNRESOLVED_TYPES";
            public const string NoName = "TS_NO_NAME_DEFINED";
            public const string NoFieldType = "TS_NO_FIELD_TYPE";
            public const string ArgumentValueTypeWrong = "TS_ARG_VALUE_TYPE_WRONG";
            public const string InvalidArgument = "TS_INVALID_ARG";
            public const string NonNullArgument = "TS_ARG_NON_NULL";
        }
    }
}
