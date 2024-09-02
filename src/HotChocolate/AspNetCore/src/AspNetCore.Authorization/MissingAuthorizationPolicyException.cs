namespace HotChocolate.AspNetCore.Authorization;

internal sealed class MissingAuthorizationPolicyException(string policyName)
    : Exception($"The policy `{policyName}` does not exist.")
{
    public string PolicyName { get; } = policyName;
}
