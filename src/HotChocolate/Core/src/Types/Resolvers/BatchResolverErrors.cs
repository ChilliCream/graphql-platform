using System.ComponentModel;
using HotChocolate.Properties;

namespace HotChocolate.Resolvers;

/// <summary>
/// Builds schema errors for batch resolvers, shared by the reflection and source-generated paths.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BatchResolverErrors
{
    /// <summary>
    /// Creates the schema error for a batch resolver method whose return type is not a list.
    /// </summary>
    public static SchemaException ReturnTypeMustBeList(Type declaringType, string methodName)
        => new(
            SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.BatchResolver_ReturnTypeMustBeList,
                    declaringType.FullName ?? declaringType.Name,
                    methodName)
                .Build());

    /// <summary>
    /// Creates the schema error for a batch resolver parameter whose collection shape is not
    /// supported.
    /// </summary>
    public static SchemaException ArgumentMustBeList(Type declaringType, string methodName, string parameterName)
        => new(
            SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.BatchResolver_ArgumentMustBeList,
                    $"{declaringType.FullName ?? declaringType.Name}.{methodName}({parameterName})")
                .Build());
}
