using System.Runtime.CompilerServices;
using HotChocolate.Language;
using static HotChocolate.Fusion.FusionResources;

namespace HotChocolate.Fusion;

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
        => new("The variable must be an argument variable.");

    public static InvalidOperationException RequestFormatter_SelectionSetEmpty()
        => new("A selection set must not be empty.");

    public static InvalidOperationException SubscriptionsMustSubscribe()
        => new("A subscription execution plan can not be executed as a query.");

    public static InvalidOperationException QueryAndMutationMustExecute()
        => new("A query or mutation execution plan can not be executed as a subscription.");

    public static SchemaException NoConfigurationProvider()
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage("No configuration provider registered.")
                .Build());

    public static SchemaException UnableToLoadConfiguration()
        => new SchemaException(
            SchemaErrorBuilder.New()
                .SetMessage("Unable to load the Fusion gateway configuration.")
                .Build());
    
    public static InvalidOperationException Node_ReadOnly()
        => new("The execution node is read-only.");
}
