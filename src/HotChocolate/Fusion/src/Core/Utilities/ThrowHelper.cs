using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion.Utilities;

internal static class ThrowHelper
{
    public static ServiceConfigurationException ServiceConfDocumentMustContainSchemaDef()
        => new(ThrowHelper_ServiceConfDocumentMustContainSchemaDef);

    public static ServiceConfigurationException ServiceConfNoClientsSpecified()
        => new(ThrowHelper_ServiceConfNoClientsSpecified);

    public static ServiceConfigurationException ServiceConfNoTypesSpecified()
        => new(ThrowHelper_ServiceConfNoTypesSpecified);

    public static ServiceConfigurationException ServiceConfInvalidValue(
        Type expected,
        IValueNode actualValue)
        => new(string.Format(
            ThrowHelper_ServiceConfInvalidValue,
            expected.Name,
            actualValue.ToString(),
            actualValue.Kind));

    public static ServiceConfigurationException ServiceConfInvalidDirectiveName(
        string expectedName,
        string actualName)
        => new(string.Format(
            ThrowHelper_ServiceConfInvalidDirectiveName,
            expectedName,
            actualName));

    public static ServiceConfigurationException ServiceConfNoDirectiveArgs(string directiveName)
        => new(string.Format(ThrowHelper_ServiceConfNoDirectiveArgs, directiveName));

    public static ServiceConfigurationException ServiceConfInvalidDirectiveArgs(
        string directiveName,
        IEnumerable<string> expectedArguments,
        IEnumerable<string> actualArguments,
        int line)
        => new(string.Format(
            ThrowHelper_ServiceConfInvalidDirectiveArgs,
            directiveName,
            string.Join(", ", expectedArguments),
            string.Join(", ", actualArguments),
            line));

    public static ArgumentException Requirement_Is_Missing(string requirement, string argumentName)
        => new(string.Format(ThrowHelper_Requirement_Is_Missing, requirement), argumentName);

    public static InvalidOperationException NoResolverInContext()
        => new(ThrowHelper_NoResolverInContext);

    public static NotSupportedException RequestFormatter_ArgumentVariableExpected()
        => new(ThrowHelper_RequestFormatter_ArgumentVariableExpected_Message);

    public static InvalidOperationException RequestFormatter_SelectionSetEmpty()
        => new(ThrowHelper_RequestFormatter_SelectionSetEmpty_Message);

    public static InvalidOperationException SubscriptionsMustSubscribe()
        => new(ThrowHelper_SubscriptionsMustSubscribe_Message);

    public static InvalidOperationException QueryAndMutationMustExecute()
        => new(ThrowHelper_QueryAndMutationMustExecute_Message);

    public static SchemaException NoConfigurationProvider()
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_NoConfigurationProvider_Message)
                .Build());

    public static SchemaException UnableToLoadConfiguration()
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage(ThrowHelper_UnableToLoadConfiguration_Message)
                .Build());

    public static InvalidOperationException Node_ReadOnly()
        => new(ThrowHelper_Node_ReadOnly_Message);

    public static InvalidOperationException UnableToCreateQueryPlan()
        => new(ThrowHelper_UnableToCreateQueryPlan_Message);

    public static InvalidOperationException NoRootNode()
        => new(ThrowHelper_NoRootNode_Message);
}
