using System.Net;
using HotChocolate.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Properties;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

internal static class ErrorHelper
{
    public static OperationResult RequestTimeout(TimeSpan timeout)
    {
        var result = OperationResult.FromError(
            new Error
            {
                Message = string.Format("The request exceeded the configured timeout of `{0}`.", timeout),
                Extensions = ImmutableOrderedDictionary<string, object?>.Empty.Add("code", ErrorCodes.Execution.Timeout)
            });

        result.ContextData = result.ContextData.Add(
            ExecutionContextData.HttpStatusCode,
            HttpStatusCode.InternalServerError);

        return result;
    }

    public static OperationResult StateInvalidForOperationPlanCache()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The operation plan cache requires a operation document hash.")
                .SetCode(ErrorCodes.Execution.OperationDocumentNotFound)
                .Build());

    public static OperationResult StateInvalidForVariableCoercion()
        => OperationResult.FromError(
            ErrorBuilder.New()
                .SetMessage("The variable coercion requires an operation execution plan.")
                .Build());

    public static IError InvalidNodeIdFormat(string originalValue)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.NodeFieldExecutionNode_InvalidNodeIdFormat)
            .SetExtension("originalValue", originalValue)
            .Build();

    /// <summary>
    /// Builds the client-facing error for a denied authorization policy, together with the
    /// correlation id and denial details that the operator diagnostics surface consumes.
    /// </summary>
    /// <param name="path">
    /// The path of the denied result element.
    /// </param>
    /// <param name="behavior">
    /// The configured denial behavior. An <see cref="PolicyDenialBehavior.Abort"/> denial
    /// never carries a path, since it short-circuits the response before the path can be
    /// resolved to concrete list positions.
    /// </param>
    /// <param name="policyName">
    /// The name (or combined expression) of the policy that denied access. Not exposed on
    /// the client error, kept only for the correlated diagnostics.
    /// </param>
    /// <param name="reason">
    /// The policy-supplied denial reason. Not exposed on the client error, kept only for
    /// the correlated diagnostics.
    /// </param>
    public static PolicyDenialError PolicyDenied(
        Path path,
        PolicyDenialBehavior behavior,
        string policyName,
        string? reason)
    {
        var reasonId = Guid.NewGuid();

        var builder = ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_PolicyDenied)
            .SetCode(FusionExecutionErrorCodes.UnauthorizedFieldOrType)
            .SetExtension("reasonId", reasonId.ToString());

        // ABORT denials short-circuit before real list positions can be resolved, and root
        // denials have no field position at all, so neither carries a path.
        if (behavior is not PolicyDenialBehavior.Abort && !path.IsRoot)
        {
            builder.SetPath(path);
        }

        return new PolicyDenialError(builder.Build(), reasonId, policyName, reason);
    }

    public static IError PolicyExecutionFailed()
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_PolicyExecutionFailed)
            .SetCode(FusionExecutionErrorCodes.UnauthorizedFieldOrType)
            .Build();

    public static IError ReservedVariablePrefix(
        VariableDefinitionNode variableDefinition,
        string variableName)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_ReservedVariablePrefix, variableName)
            .AddLocation(variableDefinition)
            .SetCode(FusionExecutionErrorCodes.ReservedVariablePrefix)
            .SetExtension("variableName", variableName)
            .Build();

    public static IError ReservedVariablePrefixInFragment(
        VariableDefinitionNode variableDefinition,
        string variableName)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_ReservedVariablePrefixInFragment, variableName)
            .AddLocation(variableDefinition)
            .SetCode(FusionExecutionErrorCodes.ReservedVariablePrefix)
            .SetExtension("variableName", variableName)
            .Build();

    public static IError ReservedVariablePrefixUsage(
        VariableNode variable,
        string variableName)
        => ErrorBuilder.New()
            .SetMessage(FusionExecutionResources.ErrorHelper_ReservedVariablePrefixUsage, variableName)
            .AddLocation(variable)
            .SetCode(FusionExecutionErrorCodes.ReservedVariablePrefix)
            .SetExtension("variableName", variableName)
            .Build();
}

/// <summary>
/// The client-facing error for a denied authorization policy, together with the
/// correlation id and denial details that identify the same denial in the operator
/// diagnostics surface.
/// </summary>
/// <param name="Error">
/// The error to surface to the client. Carries the correlation id but none of the
/// policy details below.
/// </param>
/// <param name="ReasonId">
/// The correlation id embedded in <see cref="Error"/>'s extensions, repeated here so it
/// can be attached to the corresponding diagnostics record.
/// </param>
/// <param name="PolicyName">
/// The name (or combined expression) of the policy that denied access.
/// </param>
/// <param name="Reason">
/// The policy-supplied denial reason, if any.
/// </param>
internal readonly record struct PolicyDenialError(
    IError Error,
    Guid ReasonId,
    string PolicyName,
    string? Reason);
