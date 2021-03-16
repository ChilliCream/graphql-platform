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
            public const string NonNullViolation = "HC0018";
            public const string MustBeInputType = "HC0017";
            public const string InvalidType = "HC0016";
            public const string QueryNotFound = "HC0015";

            /// <summary>
            /// A persisted query was not found when using the active persisted query pipeline.
            /// </summary>
            public const string PersistedQueryNotFound = "HC0020";
            public const string TaskProcessingError = "HC0008";
            public const string SyntaxError = "HC0014";
            public const string CannotCreateRootValue = "HC0019";
        }

        public static class Server
        {
            public const string RequestInvalid = "HC0009";
            public const string MaxRequestSize = "HC0010";
            public const string SyntaxError = "HC0011";
            public const string UnexpectedRequestParserError = "HC0012";
            public const string QueryAndIdMissing = "HC0013";

            /// <summary>
            /// At least an 'operations' field and a 'map' field need to be present.
            /// </summary>
            public const string MultiPartFormIncomplete = "HC0033";

            /// <summary>
            /// No 'operations' specified.
            /// </summary>
            public const string MultiPartNoOperationsSpecified = "HC0034";

            /// <summary>
            /// Misordered multipart fields; 'map' should follow 'operations'.
            /// </summary>
            public const string MultiPartFieldsMisordered = "HC0035";
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

        public static class Filtering
        {
            public const string FilterObjectType = "FILTER_OBJECT_TYPE";
            public const string FilterFieldDescriptorType = "FILTER_FIELD_DESCRIPTOR_TYPE";
        }

        public static class Stitching
        {
            public const string HttpRequestException = "HC0006";

            public const string UnknownRequestException = "HC0007";

            public const string ArgumentNotDefined = "STITCHING_ARG_NOT_DEFINED";
            public const string FieldNotDefined = "STITCHING_FLD_NOT_DEFINED";
            public const string VariableNotDefined = "STITCHING_VAR_NOT_DEFINED";
            public const string ScopeNotDefined = "STITCHING_SCOPE_NOT_DEFINED";
            public const string TypeNotDefined = "STITCHING_TYPE_NOT_DEFINED";
            public const string ArgumentNotFound = "STITCHING_DEL_ARGUMENT_NOT_FOUND";
        }

        public static class Spatial
        {
            /// <summary>
            /// The coordinate reference system is not supported by this server
            /// </summary>
            public const string UnknowCrs = "HC0029";

            /// <summary>
            /// Coordinates with M values cannot be reprojected
            /// </summary>
            public const string CoordinateMNotSupported = "HC0030";
        }

        public static class Data
        {
            public const string NonNullError = "HC0026";
            public const string ListNotSupported = "HC0021";
            public const string MoreThanOneElement = "HC0022";
            public const string FilteringProjectionFailed = "HC0023";
            public const string SortingProjectionFailed = "HC0024";
            public const string NoPagingationProviderFound = "HC0025";

            /// <summary>
            /// Type does not contain a valid node field. Only `items` and `nodes` are supported
            /// </summary>
            public const string NodeFieldWasNotFound = "HC0028";
        }

        public static class Types
        {
            /// <summary>
            /// Unable to infer the element type from the current resolver.
            /// This often happens if the resolver is not an iterable type like
            /// IEnumerable, IQueryable, IList etc. Ensure that you either
            /// explicitly specify the element type or that the return type of your resolver
            /// is an iterable type.
            /// </summary>
            public const string NodeTypeUnkown = "HC0031";

            /// <summary>
            /// The element schema type for pagination must be a valid GraphQL output type
            /// (ObjectType, InterfaceType, UnionType, EnumType, ScalarType).
            /// </summary>
            public const string SchemaTypeInvalid = "HC0032";
        }
    }
}
