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
}
