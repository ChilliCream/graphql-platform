using ChilliCream.Nitro.Client;
using ChilliCream.Nitro.Client.Apis;
using ChilliCream.Nitro.CommandLine.Services.Sessions;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Apis;

internal static class ApiCommandTestHelper
{
    public static ICreateApiCommandMutation_PushWorkspaceChanges CreateCreateApiResult(
        string id,
        string name,
        IReadOnlyList<string> path,
        bool treatDangerousAsBreaking = false,
        bool allowBreakingSchemaChanges = false)
    {
        var detail = CreateApiDetailMock(
            id,
            name,
            path,
            treatDangerousAsBreaking,
            allowBreakingSchemaChanges);

        var result = detail.As<ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Result_Api>();

        var change = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges_Changes>();
        change.SetupGet(x => x.Error)
            .Returns((ICreateApiCommandMutation_PushWorkspaceChanges_Changes_Error?)null);
        change.SetupGet(x => x.Result).Returns(result.Object);

        var payload = new Mock<ICreateApiCommandMutation_PushWorkspaceChanges>();
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ICreateApiCommandMutation_PushWorkspaceChanges_Errors>?)null);
        payload.SetupGet(x => x.Changes).Returns([change.Object]);
        return payload.Object;
    }

    public static IDeleteApiCommandMutation_DeleteApiById CreateDeleteApiResult(
        string id,
        string name,
        IReadOnlyList<string> path,
        bool treatDangerousAsBreaking = false,
        bool allowBreakingSchemaChanges = false)
    {
        var detail = CreateApiDetailMock(
            id,
            name,
            path,
            treatDangerousAsBreaking,
            allowBreakingSchemaChanges);

        var api = detail.As<IDeleteApiCommandMutation_DeleteApiById_Api>();
        var payload = new Mock<IDeleteApiCommandMutation_DeleteApiById>();
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<IDeleteApiCommandMutation_DeleteApiById_Errors>?)null);
        payload.SetupGet(x => x.Api).Returns(api.Object);
        return payload.Object;
    }

    public static ISetApiSettingsCommandMutation_UpdateApiSettings CreateSetApiSettingsResult(
        string id,
        string name,
        IReadOnlyList<string> path,
        bool treatDangerousAsBreaking = false,
        bool allowBreakingSchemaChanges = false)
    {
        var detail = CreateApiDetailMock(
            id,
            name,
            path,
            treatDangerousAsBreaking,
            allowBreakingSchemaChanges);

        var api = detail.As<ISetApiSettingsCommandMutation_UpdateApiSettings_Api>();
        var payload = new Mock<ISetApiSettingsCommandMutation_UpdateApiSettings>();
        payload.SetupGet(x => x.Errors)
            .Returns((IReadOnlyList<ISetApiSettingsCommandMutation_UpdateApiSettings_Errors>?)null);
        payload.SetupGet(x => x.Api).Returns(api.Object);
        return payload.Object;
    }

    public static IShowApiCommandQuery_Node_Api CreateShowApiNode(
        string id,
        string name,
        IReadOnlyList<string> path,
        bool treatDangerousAsBreaking = false,
        bool allowBreakingSchemaChanges = false)
    {
        var detail = CreateApiDetailMock(
            id,
            name,
            path,
            treatDangerousAsBreaking,
            allowBreakingSchemaChanges);

        return detail.As<IShowApiCommandQuery_Node_Api>().Object;
    }

    public static IListApiCommandQuery_WorkspaceById_Apis_Edges_Node CreateListApiNode(
        string id,
        string name,
        IReadOnlyList<string> path,
        bool treatDangerousAsBreaking = false,
        bool allowBreakingSchemaChanges = false)
    {
        var detail = CreateApiDetailMock(
            id,
            name,
            path,
            treatDangerousAsBreaking,
            allowBreakingSchemaChanges);

        return detail.As<IListApiCommandQuery_WorkspaceById_Apis_Edges_Node>().Object;
    }

    public static IDeleteApiCommandQuery_Node_Api CreateDeleteApiSelection(string name)
    {
        var node = new Mock<IDeleteApiCommandQuery_Node_Api>();
        node.SetupGet(x => x.Name).Returns(name);
        return node.Object;
    }

    public static CommandTestHost CreateHost(
        Mock<IApisClient> client,
        TestSessionService? session = null)
    {
        var host = new CommandTestHost()
            .AddService<IApisClient>(client.Object)
            .AddService<ISessionService>(session ?? TestSessionService.WithWorkspace());

        return host;
    }

    private static Mock<IApiDetailPrompt_Api> CreateApiDetailMock(
        string id,
        string name,
        IReadOnlyList<string> path,
        bool treatDangerousAsBreaking,
        bool allowBreakingSchemaChanges)
    {
        var workspace = new Mock<IShowApiCommandQuery_Node_Workspace_1>();
        workspace.SetupGet(x => x.Id).Returns("ws-1");
        workspace.SetupGet(x => x.Name).Returns("Workspace");

        var schemaRegistry = new Mock<IShowApiCommandQuery_Node_Settings_SchemaRegistry>();
        schemaRegistry.SetupGet(x => x.TreatDangerousAsBreaking).Returns(treatDangerousAsBreaking);
        schemaRegistry.SetupGet(x => x.AllowBreakingSchemaChanges).Returns(allowBreakingSchemaChanges);

        var settings = new Mock<IShowApiCommandQuery_Node_Settings>();
        settings.SetupGet(x => x.SchemaRegistry).Returns(schemaRegistry.Object);

        var detail = new Mock<IApiDetailPrompt_Api>();
        detail.SetupGet(x => x.Id).Returns(id);
        detail.SetupGet(x => x.Name).Returns(name);
        detail.SetupGet(x => x.Path).Returns(path);
        detail.SetupGet(x => x.Workspace).Returns(workspace.Object);
        detail.SetupGet(x => x.Settings).Returns(settings.Object);
        return detail;
    }
}
