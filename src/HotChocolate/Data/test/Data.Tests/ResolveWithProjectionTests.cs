using System.Linq.Expressions;
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

    [Fact]
    public async Task AsSelector_Should_UseMemberInit_When_Type_Has_Only_NonPublic_ParameterlessConstructor()
    {
        // arrange
        var captured = new List<Expression<Func<PrivateCtorEntity, PrivateCtorEntity>>>();
        var executor = await new ServiceCollection()
            .AddSingleton(captured)
            .AddGraphQL()
            .AddQueryType<PrivateCtorCaptureQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await executor.ExecuteAsync(
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
        var selector = Assert.Single(captured);
        Assert.IsType<MemberInitExpression>(selector.Body);
    }

    [Fact]
    public async Task AsSelector_Should_UseConstructor_When_Type_Has_Only_NonPublic_CoveringConstructor()
    {
        // arrange
        var captured = new List<Expression<Func<CoveringCtorEntity, CoveringCtorEntity>>>();
        var executor = await new ServiceCollection()
            .AddSingleton(captured)
            .AddGraphQL()
            .AddQueryType<CoveringCtorCaptureQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        await executor.ExecuteAsync(
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
        var selector = Assert.Single(captured);
        var newExpression = Assert.IsType<NewExpression>(selector.Body);
        Assert.NotNull(newExpression.Constructor);
        Assert.False(newExpression.Constructor.IsPublic);
    }

    [Fact]
    public async Task AsSelector_Should_ExecuteInMemory_When_Type_Has_Only_NonPublic_CoveringConstructor()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<CoveringCtorQuery>()
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

    public class PrivateCtorCaptureQuery
    {
        public IQueryable<PrivateCtorEntity> GetItems(
            ISelection selection,
            [Service] List<Expression<Func<PrivateCtorEntity, PrivateCtorEntity>>> captured)
        {
            captured.Add(selection.AsSelector<PrivateCtorEntity>());
            return new[] { PrivateCtorEntity.Create(1, "Foo") }.AsQueryable();
        }
    }

    public class CoveringCtorQuery
    {
        public IQueryable<CoveringCtorEntity> GetItems(ISelection selection)
            =>
            new[] { CoveringCtorEntity.Create(1, "Foo") }
                .AsQueryable()
                .Select(selection.AsSelector<CoveringCtorEntity>());
    }

    public class CoveringCtorCaptureQuery
    {
        public IQueryable<CoveringCtorEntity> GetItems(
            ISelection selection,
            [Service] List<Expression<Func<CoveringCtorEntity, CoveringCtorEntity>>> captured)
        {
            captured.Add(selection.AsSelector<CoveringCtorEntity>());
            return new[] { CoveringCtorEntity.Create(1, "Foo") }.AsQueryable();
        }
    }

    public class CoveringCtorEntity
    {
        private CoveringCtorEntity(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; private set; }

        public string Name { get; private set; } = default!;

        public static CoveringCtorEntity Create(int id, string name)
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
