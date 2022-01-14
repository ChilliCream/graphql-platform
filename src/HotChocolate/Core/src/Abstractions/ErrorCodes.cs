using System.Collections.Generic;

namespace HotChocolate;

public static class ErrorCodes
{
    public static class Authentication
    {
        public const string NotAuthorized = "AUTH_NOT_AUTHORIZED";
        public const string NotAuthenticated = "AUTH_NOT_AUTHENTICATED";
        public const string NoDefaultPolicy = "AUTH_NO_DEFAULT_POLICY";
        public const string PolicyNotFound = "AUTH_POLICY_NOT_FOUND";
    }

    public static class Types
    {
        public const string ReservedTypeName = "HC0061";
    }

    public static class Execution
    {
        public const string CannotSerialize = "EXEC_BATCH_VAR_SERIALIZE";
        public const string CannotSerializeLeafValue = "EXEC_INVALID_LEAF_VALUE";
        public const string CannotResolveAbstractType = "EXEC_UNABLE_TO_RESOLVE_ABSTRACT_TYPE";
        public const string ListTypeNotSupported = "EXEC_LIST_TYPE_NOT_SUPPORTED";
        public const string AutoMapVarError = "EXEC_BATCH_AUTO_MAP_VAR_TYPE";

        /// <summary>
        /// The request exceeded the configured timeout.
        /// </summary>
        public const string Timeout = "HC0045";

        /// <summary>
        /// The request was canceled
        /// </summary>
        public const string Canceled = "HC0049";

        /// <summary>
        /// The operation complexity was exceeded.
        /// </summary>
        public const string ComplexityExceeded = "HC0047";

        /// <summary>
        /// The analyzer needs a documentId, operationId, document and coerced variables.
        /// </summary>
        public const string ComplexityStateInvalid = "HC0048";

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

        /// <summary>
        /// Cannot cast the parent type to the resolver requested type.
        /// </summary>
        public const string CannotCastParent = "HC0053";

        /// <summary>
        /// The Oneof Input Objects `{0}` require that exactly one field must be supplied and that
        /// field must not be `null`. Oneof Input Objects are a special variant of Input Objects 
        /// where the type system asserts that exactly one of the fields must be set and non-null.
        /// </summary>
        public const string OneOfNoFieldSet = "HC0054";

        /// <summary>
        /// More than one field of the Oneof Input Object `{0}` is set. Oneof Input Objects 
        /// are a special variant of Input Objects where the type system asserts that exactly 
        /// one of the fields must be set and non-null.
        /// </summary>
        public const string OneOfMoreThanOneFieldSet = "HC0055";

        /// <summary>
        /// `null` was set to the field `{0}`of the Oneof Input Object `{1}`. Oneof Input Objects 
        /// are a special variant of Input Objects where the type system asserts that exactly 
        /// one of the fields must be set and non-null.
        /// </summary>
        public const string OneOfFieldIsNull = "HC0056";

        /// <summary>
        /// Value for oneof field {field.FieldName} must be non-null.
        /// </summary>
        public const string OneOfFieldMustBeNonNull = "HC0057";
    }

    /// <summary>
    /// The server error codes.
    /// </summary>
    public static class Server
    {
        public const string RequestInvalid = "HC0009";
        public const string MaxRequestSize = "HC0010";
        public const string SyntaxError = "HC0011";
        public const string UnexpectedRequestParserError = "HC0012";
        public const string QueryAndIdMissing = "HC0013";

        /// <summary>
        /// The multipart form could not be read.
        /// </summary>
        public const string MultiPartInvalidForm = "HC0033";

        /// <summary>
        /// No 'operations' specified.
        /// </summary>
        public const string MultiPartNoOperationsSpecified = "HC0034";

        /// <summary>
        /// Misordered multipart fields; 'map' should follow 'operations'.
        /// </summary>
        public const string MultiPartFieldsMisordered = "HC0035";

        /// <summary>
        /// No object paths specified for a key in the 'map'.
        /// </summary>
        public const string MultiPartNoObjectPath = "HC0037";

        /// <summary>
        /// A key is referring to a file that was not provided.
        /// </summary>
        public const string MultiPartFileMissing = "HC00038";

        /// <summary>
        /// The variable path is referring to a variable that does not exist.
        /// </summary>
        public const string MultiPartVariableNotFound = "HC0039";

        /// <summary>
        /// No object paths specified for key in 'map'.
        /// </summary>
        public const string MultiPartVariableStructureInvalid = "HC0040";

        /// <summary>
        /// Invalid variable path in `map`.
        /// </summary>
        public const string MultiPartInvalidPath = "HC0041";

        /// <summary>
        /// The variable path must start with `variables`.
        /// </summary>
        public const string MultiPartPathMustStartWithVariable = "HC0042";

        /// <summary>
        /// Invalid JSON in the `map` multipart field; Expected type of
        /// <see cref="Dictionary{TKey,TValue}" />.
        /// </summary>
        public const string MultiPartInvalidMapJson = "HC0043";

        /// <summary>
        /// No `map` specified.
        /// </summary>
        public const string MultiPartMapNotSpecified = "HC0044";

        /// <summary>
        /// The type parameter is empty.
        /// </summary>
        public const string TypeParameterIsEmpty = "HC0058";

        /// <summary>
        /// Invalid type name when requesting the SDL.
        /// </summary>
        public const string InvalidTypeName = "HC0059";

        /// <summary>
        /// The requested type does not exist.
        /// </summary>
        public const string TypeDoesNotExist = "HC0060";
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

        /// <summary>
        /// The middleware order of a field pipeline is incorrect.
        /// </summary>
        public const string MiddlewareOrderInvalid = "HC0050";

        /// <summary>
        /// Type system members are not supported as runtime types
        /// </summary>
        public const string NoSchemaTypesAllowedAsRuntimeType = "HC0051";
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
        public const string NoPaginationProviderFound = "HC0025";

        /// <summary>
        /// Type does not contain a valid node field. Only `items` and `nodes` are supported
        /// </summary>
        public const string NodeFieldWasNotFound = "HC0028";
    }

    /// <summary>
    /// Error codes relating to the document validation.
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// The introspection is not allowed for the current request
        /// </summary>
        public const string IntrospectionNotAllowed = "HC0046";
    }

    /// <summary>
    /// Error codes related to paging compinents
    /// </summary>
    public static class Paging
    {
        /// <summary>
        /// Unable to infer the element type from the current resolver.
        /// This often happens if the resolver is not an iterable type like
        /// IEnumerable, IQueryable, IList etc. Ensure that you either
        /// explicitly specify the element type or that the return type of your resolver
        /// is an iterable type.
        /// </summary>
        public const string NodeTypeUnknown = "HC0031";

        /// <summary>
        /// The element schema type for pagination must be a valid GraphQL output type
        /// (ObjectType, InterfaceType, UnionType, EnumType, ScalarType).
        /// </summary>
        public const string SchemaTypeInvalid = "HC0032";

        /// <summary>
        /// The maximum allowed items per page were exceeded.
        /// </summary>
        public const string MaxPaginationItems = "HC0051";

        /// <summary>
        /// You must provide a `first` or `last` value to properly paginate the connection.
        /// </summary>
        public const string NoPagingBoundaries = "HC0052";
    }
}
