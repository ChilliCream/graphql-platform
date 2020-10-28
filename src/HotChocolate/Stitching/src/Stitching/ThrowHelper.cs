using System;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching
{
    internal static class ThrowHelper
    {
        public static InvalidOperationException BufferedRequest_VariableDoesNotExist(
            string name) =>
            new InvalidOperationException(string.Format(
                ThrowHelper_BufferedRequest_VariableDoesNotExist,
                name));

        public static InvalidOperationException BufferedRequest_OperationNotFound(
            DocumentNode document) =>
            new InvalidOperationException(string.Format(
                ThrowHelper_BufferedRequest_OperationNotFound,
                document));

        public static GraphQLException ArgumentScopedVariableResolver_InvalidArgumentName(
            string variableName,
            FieldNode fieldSelection,
            Path path) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.ArgumentScopedVariableResolver_InvalidArgumentName,
                    variableName)
                .SetCode(ErrorCodes.Stitching.ArgumentNotDefined)
                .SetPath(path)
                .AddLocation(fieldSelection)
                .Build());

        public static GraphQLException FieldScopedVariableResolver_InvalidFieldName(
            string variableName,
            FieldNode fieldSelection,
            Path path) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.FieldScopedVariableResolver_InvalidFieldName,
                    variableName)
                .SetCode(ErrorCodes.Stitching.FieldNotDefined)
                .SetPath(path)
                .AddLocation(fieldSelection)
                .Build());

        public static GraphQLException RootScopedVariableResolver_ScopeNotSupported(
            string scopeName,
            FieldNode fieldSelection,
            Path path) =>
            new GraphQLException(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.RootScopedVariableResolver_ScopeNotSupported,
                    scopeName)
                .SetCode(ErrorCodes.Stitching.ScopeNotDefined)
                .SetPath(path)
                .AddLocation(fieldSelection)
                .Build());
    }
}
