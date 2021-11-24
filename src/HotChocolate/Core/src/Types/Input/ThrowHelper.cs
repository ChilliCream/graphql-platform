using System;
using HotChocolate.Properties;
using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Input
{
    public static class ThrowHelper
    {
        public static Exception ArgumentTypeNameMissMatch(
            ObjectTypeDefinition objectTypeDefinition,
            string generatedArgumentName,
            ObjectFieldDefinition fieldDefinition,
            string currentTypeName,
            string collidingTypeName) =>
            new SchemaException(SchemaErrorBuilder.New()
                .SetMessage(
                    TypeResources.ThrowHelper_InputMiddleware_ArgumentTypeNameMissMatch,
                    generatedArgumentName,
                    $"{objectTypeDefinition.Name}.{fieldDefinition.Name}",
                    currentTypeName,
                    collidingTypeName,
                    objectTypeDefinition.Name)
                .Build());

    }
}
