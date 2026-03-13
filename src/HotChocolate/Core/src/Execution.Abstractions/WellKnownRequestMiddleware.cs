namespace HotChocolate.Execution;

public static class WellKnownRequestMiddleware
{
    /// <summary>Gets the key for the QueryCacheMiddleware.</summary>
    public const string QueryCacheMiddleware = "QueryCacheMiddleware";

    /// <summary>Gets the key for the DocumentCacheMiddleware.</summary>
    public const string DocumentCacheMiddleware = "DocumentCacheMiddleware";

    /// <summary>Gets the key for the DocumentParserMiddleware.</summary>
    public const string DocumentParserMiddleware = "DocumentParserMiddleware";

    /// <summary>Gets the key for the DocumentValidationMiddleware.</summary>
    public const string DocumentValidationMiddleware = "DocumentValidationMiddleware";

    /// <summary>Gets the key for the ExceptionMiddleware.</summary>
    public const string ExceptionMiddleware = "ExceptionMiddleware";

    /// <summary>Gets the key for the InstrumentationMiddleware.</summary>
    public const string InstrumentationMiddleware = "InstrumentationMiddleware";

    /// <summary>Gets the key for the SkipWarmupExecutionMiddleware.</summary>
    public const string SkipWarmupExecutionMiddleware = "SkipWarmupExecutionMiddleware";

    /// <summary>Gets the key for the OperationCacheMiddleware.</summary>
    public const string OperationCacheMiddleware = "OperationCacheMiddleware";

    /// <summary>Gets the key for the OperationExecutionMiddleware.</summary>
    public const string OperationExecutionMiddleware = "OperationExecutionMiddleware";

    /// <summary>Gets the key for the OperationResolverMiddleware.</summary>
    public const string OperationResolverMiddleware = "OperationResolverMiddleware";

    /// <summary>Gets the key for the OperationVariableCoercionMiddleware.</summary>
    public const string OperationVariableCoercionMiddleware = "OperationVariableCoercionMiddleware";

    /// <summary>Gets the key for the TimeoutMiddleware.</summary>
    public const string TimeoutMiddleware = "TimeoutMiddleware";

    /// <summary>Gets the key for the AutomaticPersistedOperationNotFoundMiddleware.</summary>
    public const string AutomaticPersistedOperationNotFoundMiddleware =
        "AutomaticPersistedOperationNotFoundMiddleware";

    /// <summary>Gets the key for the OnlyPersistedOperationsAllowed.</summary>
    public const string OnlyPersistedOperationsAllowed = "OnlyPersistedOperationsAllowed";

    /// <summary>Gets the key for the PersistedOperationNotFoundMiddleware.</summary>
    public const string PersistedOperationNotFoundMiddleware = "PersistedOperationNotFoundMiddleware";

    /// <summary>Gets the key for the ReadPersistedOperationMiddleware.</summary>
    public const string ReadPersistedOperationMiddleware = "ReadPersistedOperationMiddleware";

    /// <summary>Gets the key for the WritePersistedOperationMiddleware.</summary>
    public const string WritePersistedOperationMiddleware = "WritePersistedOperationMiddleware";

    /// <summary>Gets the key for the AuthorizeRequestMiddleware.</summary>
    public const string AuthorizeRequestMiddleware = "HotChocolate.Authorization.Pipeline.AuthorizeRequest";

    /// <summary>Gets the key for the PrepareAuthorizationMiddleware.</summary>
    public const string PrepareAuthorizationMiddleware = "HotChocolate.Authorization.Pipeline.PrepareAuthorization";

    /// <summary>Gets the key for the CostAnalyzerMiddleware.</summary>
    public const string CostAnalyzerMiddleware = "CostAnalyzerMiddleware";

    /// <summary>Gets the key for the OperationPlanCacheMiddleware.</summary>
    public const string OperationPlanCacheMiddleware = "OperationPlanCacheMiddleware";

    /// <summary>Gets the key for the OperationPlanMiddleware.</summary>
    public const string OperationPlanMiddleware = "OperationPlanMiddleware";
}
