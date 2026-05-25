using ChilliCream.Nitro.Client;
using Moq;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.PersonalAccessTokens;

public abstract class PersonalAccessTokensCommandTestBase(NitroCommandFixture fixture)
    : CommandTestBase(fixture)
{
    protected const string PatId = "pat-1";
    protected const string PatDescription = "my-token";
    protected const string PatSecret = "secret-123";

    #region Create

    protected void SetupCreatePersonalAccessTokenMutation(
        params ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors[] errors)
    {
        PersonalAccessTokensClientMock.Setup(x => x.CreatePersonalAccessTokenAsync(
                PatDescription,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? CreateCreatePatPayloadWithErrors(errors)
                : CreateCreatePatPayload());
    }

    protected void SetupCreatePersonalAccessTokenMutationException()
    {
        PersonalAccessTokensClientMock.Setup(x => x.CreatePersonalAccessTokenAsync(
                PatDescription,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupCreatePersonalAccessTokenMutationNullResult()
    {
        PersonalAccessTokensClientMock.Setup(x => x.CreatePersonalAccessTokenAsync(
                PatDescription,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateCreatePatPayloadWithNullResult());
    }

    #endregion

    #region List

    protected void SetupListPersonalAccessTokensQuery(
        string? cursor = null,
        int first = 10,
        string? endCursor = null,
        bool hasNextPage = false,
        params (string Id, string Description, DateTimeOffset CreatedAt, DateTimeOffset ExpiresAt)[] tokens)
    {
        var items = tokens
            .Select(static t =>
                (IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node)
                new ListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node_PersonalAccessToken(
                    t.Id, t.Description, t.ExpiresAt, t.CreatedAt))
            .ToArray();

        PersonalAccessTokensClientMock.Setup(x => x.ListPersonalAccessTokensAsync(
                cursor, first, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConnectionPage<IListPersonalAccessTokenCommandQuery_Me_PersonalAccessTokens_Edges_Node>(
                items, endCursor, hasNextPage));
    }

    protected void SetupListPersonalAccessTokensQueryException()
    {
        PersonalAccessTokensClientMock.Setup(x => x.ListPersonalAccessTokensAsync(
                null, 10, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    #endregion

    #region Revoke

    protected void SetupRevokePersonalAccessTokenMutation(
        params IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors[] errors)
    {
        PersonalAccessTokensClientMock.Setup(x => x.RevokePersonalAccessTokenAsync(
                PatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(errors.Length > 0
                ? CreateRevokePatPayloadWithErrors(errors)
                : CreateRevokePatPayload());
    }

    protected void SetupRevokePersonalAccessTokenMutationException()
    {
        PersonalAccessTokensClientMock.Setup(x => x.RevokePersonalAccessTokenAsync(
                PatId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Something unexpected happened."));
    }

    protected void SetupRevokePersonalAccessTokenMutationNullPersonalAccessToken()
    {
        PersonalAccessTokensClientMock.Setup(x => x.RevokePersonalAccessTokenAsync(
                PatId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRevokePatPayloadWithNullResult());
    }

    #endregion

    #region Error Factories -- CreatePersonalAccessToken

    protected static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors
        CreateCreatePersonalAccessTokenUnauthorizedError()
    {
        return new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_UnauthorizedOperation(
            "UnauthorizedOperation", "Not authorized");
    }

    protected static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors
        CreateCreatePersonalAccessTokenValidationError()
    {
        return new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors_ValidationError(
            "Validation failed");
    }

    #endregion

    #region Error Factories -- RevokePersonalAccessToken

    protected static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors
        CreateRevokePersonalAccessTokenNotFoundError()
    {
        return new RevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors_PersonalAccessTokenNotFoundError(
            "PersonalAccessTokenNotFoundError", "PAT not found");
    }

    protected static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors
        CreateRevokePersonalAccessTokenUnauthorizedError()
    {
        return new RevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors_UnauthorizedOperation(
            "Not authorized");
    }

    #endregion

    #region Payload Factories

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreateCreatePatPayload()
    {
        var token = new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result_Token_PersonalAccessToken(
            PatId,
            PatDescription,
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var resultObj = new CreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result_PersonalAccessTokenWithSecret(
            token, PatSecret);

        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.Result).Returns(resultObj);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreateCreatePatPayloadWithNullResult()
    {
        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.Result)
            .Returns((ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken CreateCreatePatPayloadWithErrors(
        params ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Errors[] errors)
    {
        var payload = new Mock<ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.Result)
            .Returns((ICreatePersonalAccessTokenCommandMutation_CreatePersonalAccessToken_Result?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateRevokePatPayload()
    {
        var token = new RevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken_PersonalAccessToken(
            PatId,
            PatDescription,
            new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.PersonalAccessToken).Returns(token);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateRevokePatPayloadWithNullResult()
    {
        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.PersonalAccessToken)
            .Returns((IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken?)null);
        payload.SetupGet(x => x.Errors)
            .Returns(Array.Empty<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors>());
        return payload.Object;
    }

    private static IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken CreateRevokePatPayloadWithErrors(
        params IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_Errors[] errors)
    {
        var payload = new Mock<IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken>(MockBehavior.Strict);
        payload.SetupGet(x => x.PersonalAccessToken)
            .Returns((IRevokePersonalAccessTokenCommandMutation_RevokePersonalAccessToken_PersonalAccessToken?)null);
        payload.SetupGet(x => x.Errors).Returns(errors);
        return payload.Object;
    }

    #endregion
}
