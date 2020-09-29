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

        public static class Execution
        {
            public const string CannotSerialize = "EXEC_BATCH_VAR_SERIALIZE";
            public const string CannotSerializeLeafValue = "EXEC_INVALID_LEAF_VALUE";
            public const string CannotResolveAbstractType = "EXEC_UNABLE_TO_RESOLVE_ABSTRACT_TYPE";
            public const string ListTypeNotSupported = "EXEC_LIST_TYPE_NOT_SUPPORTED";
            public const string AutoMapVarError = "EXEC_BATCH_AUTO_MAP_VAR_TYPE";
            public const string Incomplete = "EXEC_MIDDLEWARE_INCOMPLETE";
            public const string Timeout = "EXEC_TIMEOUT";
            public const string NonNullViolation = "EXEC_NON_NULL_VIOLATION";
            public const string MustBeInputType = "EXEC_INPUT_TYPE_REQUIRED";
            public const string InvalidType = "EXEC_INVALID_TYPE";
            public const string SyntaxError = "EXEC_SYNTAX_ERROR";
            public const string QueryNotFound = "QUERY_NOT_FOUND";
        }

        public static class Schema
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
            public const string InterfaceNotImplemented = "SCHEMA_INTERFACE_NO_IMPL";
        }

        public static class Scalars
        {
            /// <summary>
            /// The runtime type is not supported by the scalars ParseValue method.
            /// </summary>
            public const string InvalidRuntimeType = "HC0001";

            /// <summary>
            /// Either the syntax node is invalid when parsing the literal or the syntax
            /// node value has an invalid format.
            /// </summary>
            public const string InvalidSyntaxFormat = "HC0002";
        }

        public static class Apollo
        {
            public static class Federation
            {
                /// <summary>
                /// The key attribute is used on the type level without specifying the the
                /// field set.
                /// </summary>
                public const string KeyFieldSetNullOrEmpty = "HC0003";

                /// <summary>
                /// The provides attribute is used and the field set is set to <c>null</c> or
                /// <see cref="string.Empty"/>.
                /// </summary>
                public const string ProvidesFieldSetNullOrEmpty = "HC0004";
            }
        }

        public static class Filtering
        {
            public const string FilterObjectType = "FILTER_OBJECT_TYPE";
            public const string FilterFieldDescriptorType = "FILTER_FIELD_DESCRIPTOR_TYPE";
            public const string NoOperationNameFound = "FILTER_CONVENTION_NO_OPERATION_NAME_FOUND";
        }

        public static class Sorting
        {
            public const string SortObjectType = "SORT_OBJECT_TYPE";
        }

        public static class Serialization
        {
            public const string ResultTypeNotSupported = "RESULT_TYPE_NOT_SUPPORTED";
        }

        public static class Server
        {
            public const string RequestInvalid = "INVALID_REQUEST";
            public const string MaxRequestSize = "MAX_REQUEST_SIZE";
        }

        public static class Validation
        {
            public const string UnknownType = "VALIDATION_UNKNOWN_TYPE";
        }

        public static class Utilities
        {
            public const string UnknownField = "EXEC_VAR_UNKNOWN_FIELD";
            public const string NoConverter = "EXEC_VAR_NO_CONVERTER";
        }
    }
}
