using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class ResolveWithProjectionTests
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
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            """,
            TestContext.Current.CancellationToken);

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
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

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
            """,
            TestContext.Current.CancellationToken);

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
    public async Task AsSelector_Should_Project_When_Type_Has_Only_Private_Constructors()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PrivateCtorQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                id
                name
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "items": [
                  {
                    "id": 1,
                    "name": "Foo"
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

    public class PrivateCtorQuery
    {
        public IQueryable<PrivateCtorEntity> GetItems(ISelection selection)
            =>
            new[] { PrivateCtorEntity.Create(1, "Foo") }
                .AsQueryable()
                .Select(selection.AsSelector<PrivateCtorEntity>());
    }

    public class PrivateCtorEntity
    {
        private PrivateCtorEntity()
        {
        }

        private PrivateCtorEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; private set; }

        public string Name { get; private set; } = default!;

        public static PrivateCtorEntity Create(int id, string name)
            => new(id, name);
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
