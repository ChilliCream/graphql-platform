using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class Issue9741Tests
{
    [Fact]
    public async Task ResolveWith_Should_Project_Member_When_UseProjection_Is_Applied_On_Parent()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddProjections()
            .AddQueryType<Query>()
            .AddType<TenantType>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              tenants {
                workspaces {
                  id
                }
              }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "tenants": [
                  {
                    "workspaces": [
                      {
                        "id": 2
                      },
                      {
                        "id": 4
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ResolveWith_Should_Project_Member_When_AsSelector_Is_Used_On_Parent()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<AsSelectorQuery>()
            .AddType<TenantType>()
            .BuildRequestExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              tenants {
                workspaces {
                  id
                }
              }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "tenants": [
                  {
                    "workspaces": [
                      {
                        "id": 2
                      },
                      {
                        "id": 4
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    public class Query
    {
        [UseProjection]
        public IQueryable<Tenant> GetTenants()
            => CreateTenants().AsQueryable();
    }

    public class AsSelectorQuery
    {
        public IQueryable<Tenant> GetTenants(ISelection selection)
            => CreateTenants().AsQueryable().Select(selection.AsSelector<Tenant>());
    }

    private static Tenant[] CreateTenants()
        =>
        [
            new Tenant
            {
                Id = 1,
                Workspaces =
                [
                    new Workspace { Id = 1 },
                    new Workspace { Id = 2 },
                    new Workspace { Id = 3 },
                    new Workspace { Id = 4 }
                ]
            }
        ];

    public class TenantType : ObjectType<Tenant>
    {
        protected override void Configure(IObjectTypeDescriptor<Tenant> descriptor)
        {
            descriptor
                .Field(t => t.Workspaces)
                .ResolveWith<TenantResolvers>(r => r.GetWorkspaces(default!));
        }
    }

    public class TenantResolvers
    {
        public IQueryable<Workspace> GetWorkspaces([Parent] Tenant tenant)
            => tenant.Workspaces.Where(w => w.Id % 2 == 0).AsQueryable();
    }

    public class Tenant
    {
        public int Id { get; set; }

        public List<Workspace> Workspaces { get; set; } = [];
    }

    public class Workspace
    {
        public int Id { get; set; }
    }
}
