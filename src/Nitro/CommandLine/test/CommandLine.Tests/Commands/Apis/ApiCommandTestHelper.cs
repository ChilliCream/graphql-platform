using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

internal static class ApiCommandTestHelper
{
    public static IShowApiCommandQuery_Node CreateShowApiNode(
        string id,
        string name,
        IReadOnlyList<string> path,
        string workspaceName = "Workspace",
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
        => new ShowApiCommandQuery_Node_Api(
            id,
            name,
            path,
            CreateWorkspace(workspaceName),
            CreateSettings(treatDangerousAsBreaking, allowBreakingSchemaChanges));

    public static IDeleteApiCommandQuery_Node CreateDeleteApiNode(
        string name,
        string version = "v1")
        => new DeleteApiCommandQuery_Node_Api(name, version, workspace: null);

    public static IDeleteApiCommandMutation_DeleteApiById CreateDeleteApiPayload(
        string id,
        string name,
        IReadOnlyList<string> path,
        string workspaceName = "Workspace",
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
        => new DeleteApiCommandMutation_DeleteApiById_DeleteApiByIdPayload(
            new DeleteApiCommandMutation_DeleteApiById_Api_Api(
                name,
                id,
                path,
                CreateWorkspace(workspaceName),
                CreateSettings(treatDangerousAsBreaking, allowBreakingSchemaChanges)),
            errors: []);

    public static IDeleteApiCommandMutation_DeleteApiById CreateDeleteApiPayloadWithErrors(
        params IDeleteApiCommandMutation_DeleteApiById_Errors[] errors)
        => new DeleteApiCommandMutation_DeleteApiById_DeleteApiByIdPayload(api: null, errors);

    public static ISetApiSettingsCommandMutation_UpdateApiSettings CreateSetApiSettingsPayload(
        string id,
        string name,
        IReadOnlyList<string> path,
        string workspaceName = "Workspace",
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
        => new SetApiSettingsCommandMutation_UpdateApiSettings_UpdateApiSettingsPayload(
            new SetApiSettingsCommandMutation_UpdateApiSettings_Api_Api(
                name,
                path,
                id,
                CreateWorkspace(workspaceName),
                CreateSettings(treatDangerousAsBreaking, allowBreakingSchemaChanges)),
            errors: []);

    public static ISetApiSettingsCommandMutation_UpdateApiSettings CreateSetApiSettingsPayloadWithErrors(
        params ISetApiSettingsCommandMutation_UpdateApiSettings_Errors[] errors)
        => new SetApiSettingsCommandMutation_UpdateApiSettings_UpdateApiSettingsPayload(api: null, errors);

    public static ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node> CreateListApisPage(
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Name, IReadOnlyList<string> Path, string WorkspaceName)[] apis)
    {
        var items = apis
            .Select(static api =>
                (IListApiCommandQuery_WorkspaceById_Apis_Edges_Node)
                new ListApiCommandQuery_WorkspaceById_Apis_Edges_Node_Api(
                    api.Id,
                    api.Name,
                    api.Path,
                    CreateWorkspace(api.WorkspaceName),
                    CreateSettings()))
            .ToArray();

        return new ConnectionPage<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>(items, endCursor, hasNextPage);
    }

    private static IShowApiCommandQuery_Node_Workspace_1 CreateWorkspace(
        string name,
        string id = "ws-1")
        => new ShowApiCommandQuery_Node_Workspace_Workspace(id, name);

    private static IShowApiCommandQuery_Node_Settings CreateSettings(
        bool treatDangerousAsBreaking = true,
        bool allowBreakingSchemaChanges = false)
        => new ShowApiCommandQuery_Node_Settings_ApiSettings(
            new ShowApiCommandQuery_Node_Settings_SchemaRegistry_SchemaRegistrySettings(
                treatDangerousAsBreaking,
                allowBreakingSchemaChanges));
}
