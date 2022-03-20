using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MicrosoftAuthorize = Microsoft.AspNetCore.Authorization.AuthorizeAttribute;

namespace HotChocolate.AspNetCore.Authorization;

public class MicrosoftAuthorizeAttributeTests
{
    [Fact]
    public async Task Apply_AuthorizeDirective_To_Schema()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddAuthorization()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Apply_AuthorizeDirective_To_Schema_InferFields()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<InferRolesAndPolicyQuery>()
            .AddAuthorization()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Apply_AuthorizeDirective_To_Schema_TypeExtension()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType()
            .AddTypeExtension<QueryExtensions>()
            .AddAuthorization()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task Apply_AuthorizeDirective_To_Schema_Multiple_Attributes()
    {
        await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<MultipleAuthorizeQuery>()
            .AddAuthorization()
            .BuildSchemaAsync()
            .MatchSnapshotAsync();
    }

    public class Query
    {
        public string Field { get; set; }

        public AuthorizedType FieldWithAuthorizedType { get; set; }

        [MicrosoftAuthorize]
        public string FieldWithAuthorize()
            => default;
    }

    public class InferRolesAndPolicyQuery
    {
        public string Field { get; set; }

        [MicrosoftAuthorize]
        public string FieldWithAuthorize()
            => default;

        [MicrosoftAuthorize(Policy = "policy")]
        public string FieldWithPolicy()
            => default;

        [MicrosoftAuthorize(Roles = "role")]
        public string FieldWithRole()
            => default;

        [MicrosoftAuthorize(Roles = "role1,role2,role3")]
        public string FieldWithMultipleRoles()
            => default;

        [MicrosoftAuthorize(Roles = "role1, role2 , role3,")]
        public string FieldWithWhitespaceBetweenMultipleRoles()
            => default;

        [MicrosoftAuthorize(Roles = "role1,role2", Policy = "policy")]
        public string FieldWithRolesAndPolicy()
            => default;
    }

    [MicrosoftAuthorize(Policy = "policy1")]
    [MicrosoftAuthorize(Policy = "policy2")]
    public class MultipleAuthorizeQuery
    {
        public string Field { get; set; }

        [MicrosoftAuthorize(Policy = "policy3")]
        [MicrosoftAuthorize(Policy = "policy4")]
        public string FieldWithAuthorize()
            => default;
    }

    [MicrosoftAuthorize]
    public class AuthorizedType
    {
        public string Field { get; set; }
    }

    [ExtendObjectType((OperationTypeNames.Query))]
    [MicrosoftAuthorize]
    public class QueryExtensions
    {
        [MicrosoftAuthorize]
        public string FieldWithAuthorize()
            => default;
    }
}
