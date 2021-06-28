using System;
using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Stitching.Properties;
using static HotChocolate.Stitching.Properties.StitchingResources;

namespace HotChocolate.Stitching
{
    internal static class ThrowHelper
    {
        public static InvalidOperationException BufferedRequest_VariableDoesNotExist(
            string name) =>
            new(string.Format(
                ThrowHelper_BufferedRequest_VariableDoesNotExist,
                name));

        public static InvalidOperationException BufferedRequest_OperationNotFound(
            DocumentNode document) =>
            new(string.Format(
                ThrowHelper_BufferedRequest_OperationNotFound,
                document));

        public static GraphQLException ArgumentScopedVariableResolver_InvalidArgumentName(
            string variableName,
            FieldNode fieldSelection,
            Path path) =>
            new(ErrorBuilder.New()
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
            new(ErrorBuilder.New()
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
            new(ErrorBuilder.New()
                .SetMessage(
                    StitchingResources.RootScopedVariableResolver_ScopeNotSupported,
                    scopeName)
                .SetCode(ErrorCodes.Stitching.ScopeNotDefined)
                .SetPath(path)
                .AddLocation(fieldSelection)
                .Build());

        public static SchemaException PublishSchemaDefinitionDescriptor_ResourceNotFound(
            string key) =>
            new(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The resource `{0}` was not found!",
                        key)
                    .Build());

        public static SchemaException IntrospectionHelper_UnableToFetchSchemaDefinition(
            IReadOnlyList<IError> errors) =>
            new(
                SchemaErrorBuilder.New()
                    .SetMessage("Unable to fetch schema definition.")
                    .SetExtension("errors", errors)
                    .Build());

        public static SchemaException RequestExecutorBuilder_ResourceNotFound(
            string key) =>
            new(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "The resource `{0}` was not found!",
                        key)
                    .Build());

        public static SchemaException RequestExecutorBuilder_ArgumentWithNameWasNotFound(
            string argument) =>
            new(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "`{0}` is not specified.",
                        argument)
                    .Build());

        public static SchemaException RequestExecutorBuilder_ArgumentValueWasNotAStringValue(
            string argument) =>
            new(
                SchemaErrorBuilder.New()
                    .SetMessage(
                        "`{0}` must have a string value.",
                        argument)
                    .Build());

        public static InvalidOperationException RequestExecutorBuilder_RemoteExecutorNotFound() =>
            new("The mandatory remote executors have not been found.");

        public static InvalidOperationException RequestExecutorBuilder_NameLookupNotFound() =>
            new("A stitched schema must provide a name lookup");
    }
}
