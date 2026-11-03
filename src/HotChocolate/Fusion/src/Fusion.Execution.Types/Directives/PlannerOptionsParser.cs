using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Directives;

internal static class PlannerOptionsParser
{
    public static PlannerOptions Parse(DocumentNode document)
    {
        var executionDirectives = document.Definitions
            .SelectMany(static definition => definition switch
            {
                SchemaDefinitionNode schemaDefinition => schemaDefinition.Directives,
                SchemaExtensionNode schemaExtension => schemaExtension.Directives,
                _ => []
            })
            .Where(static directive => directive.Name.Value.Equals(
                FusionBuiltIns.Execution,
                StringComparison.Ordinal))
            .Take(2)
            .ToArray();

        if (executionDirectives.Length == 0)
        {
            return PlannerOptions.Default;
        }

        if (executionDirectives.Length > 1)
        {
            throw ThrowHelper.DuplicateExecutionDirective();
        }

        var executionDirective = executionDirectives[0];

        return new PlannerOptions(
            ParseNodeResolution(executionDirective),
            ParseShareableFieldRuntimeTypeRouting(executionDirective));
    }

    private static NodeResolution ParseNodeResolution(DirectiveNode executionDirective)
    {
        var nodeResolutionArgument = executionDirective.Arguments.FirstOrDefault(
            static argument => argument.Name.Value.Equals(
                "nodeResolution",
                StringComparison.Ordinal));

        if (nodeResolutionArgument is null)
        {
            return NodeResolution.Gateway;
        }

        return nodeResolutionArgument.Value switch
        {
            EnumValueNode { Value: "GATEWAY" } => NodeResolution.Gateway,
            EnumValueNode { Value: "SOURCE_SCHEMA" } => NodeResolution.SourceSchema,
            _ => throw ThrowHelper.InvalidExecutionArgument("nodeResolution", "GATEWAY or SOURCE_SCHEMA")
        };
    }

    private static ShareableFieldRuntimeTypeRouting ParseShareableFieldRuntimeTypeRouting(
        DirectiveNode executionDirective)
    {
        var argument = executionDirective.Arguments.FirstOrDefault(
            static argument => argument.Name.Value.Equals(
                "shareableFieldRuntimeTypeRouting",
                StringComparison.Ordinal));

        if (argument is null)
        {
            return ShareableFieldRuntimeTypeRouting.SourceLocal;
        }

        return argument.Value switch
        {
            EnumValueNode { Value: "SOURCE_LOCAL" } =>
                ShareableFieldRuntimeTypeRouting.SourceLocal,
            EnumValueNode { Value: "COMMON_RUNTIME_TYPES" } =>
                ShareableFieldRuntimeTypeRouting.CommonRuntimeTypes,
            _ => throw ThrowHelper.InvalidExecutionArgument(
                "shareableFieldRuntimeTypeRouting", "SOURCE_LOCAL or COMMON_RUNTIME_TYPES")
        };
    }
}
