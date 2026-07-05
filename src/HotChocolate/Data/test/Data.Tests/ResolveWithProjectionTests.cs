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

    [Fact]
    public async Task AsSelector_Should_Project_ReadOnly_Property_When_Covering_Constructor_Exists()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<AggregateCountryQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              countries {
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
                "countries": [
                  {
                    "name": "Foo"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task AsSelector_Should_Project_Selected_Field_When_Constructor_Validates_Unselected_Parameter()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<ValidatingCtorQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                code
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
                    "code": "ABC"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task AsSelector_Should_ProjectVerbatimValue_When_Writable_Member_Overrides_Constructor_Transform()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<TransformingCtorQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                code
                createdAt
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
                    "code": "abc",
                    "createdAt": "2024-01-01T00:00:00Z"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task AsSelector_Should_Project_Writable_Members_When_ReadOnly_Leaf_Has_No_Covering_Constructor()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<UncoveredReadOnlyLeafQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                id
                nested {
                  name
                }
                label
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
                    "nested": {
                      "name": "Nested"
                    },
                    "label": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task AsSelector_Should_Skip_Computed_ReadOnly_Property_When_Selected()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<ComputedPropertyQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items {
                id
                upperName
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
                    "upperName": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task AsSelector_Should_Project_Id_And_Name_When_Nodes_Selected_Before_Edges()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PagingCtorQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items(first: 10) {
                nodes {
                  id
                }
                edges {
                  node {
                    name
                  }
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
                "items": {
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ],
                  "edges": [
                    {
                      "node": {
                        "name": "foo"
                      }
                    },
                    {
                      "node": {
                        "name": "bar"
                      }
                    }
                  ]
                }
              }
            }
            """);
    }

    [Fact]
    public async Task AsSelector_Should_Project_Id_And_Name_When_Edges_Selected_Before_Nodes()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PagingCtorQuery>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              items(first: 10) {
                edges {
                  node {
                    name
                  }
                }
                nodes {
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
                "items": {
                  "edges": [
                    {
                      "node": {
                        "name": "foo"
                      }
                    },
                    {
                      "node": {
                        "name": "bar"
                      }
                    }
                  ],
                  "nodes": [
                    {
                      "id": 1
                    },
                    {
                      "id": 2
                    }
                  ]
                }
              }
            }
            """);
    }

    public class ValidatingCtorQuery
    {
        public IQueryable<ValidatingCtorEntity> GetItems(ISelection selection)
            => new[] { new ValidatingCtorEntity("ABC", "General") }
                .AsQueryable()
                .Select(selection.AsSelector<ValidatingCtorEntity>());
    }

    // A DDD entity whose constructor validates every argument. Category is never selected by
    // the query below, so the fix must feed it the source instance's real value instead of a
    // default, or the constructor throws for a field the caller never asked for.
    public sealed class ValidatingCtorEntity
    {
        public ValidatingCtorEntity(string code, string category)
        {
            ArgumentNullException.ThrowIfNull(category);
            Code = code;
            Category = category;
        }

        public string Code { get; } = default!;

        public string Category { get; } = default!;
    }

    public class TransformingCtorQuery
    {
        public IQueryable<TransformingCtorEntity> GetItems(ISelection selection)
        {
            // The constructor is only used by the projection. The stored entity's Code is
            // assigned directly through the public setter afterward, the same way an ORM
            // materializes a read-only leaf without re-running business constructor logic,
            // so the source data keeps the verbatim, untransformed value.
            var entity = new TransformingCtorEntity(
                "abc",
                new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));
            entity.Code = "abc";

            return new[] { entity }.AsQueryable().Select(selection.AsSelector<TransformingCtorEntity>());
        }
    }

    // Code is writable and CreatedAt is a read-only leaf, so the constructor is used to cover
    // CreatedAt. The constructor also transforms Code; the writable Code binding must be applied
    // after the constructor runs so the verbatim, untransformed value wins.
    public sealed class TransformingCtorEntity
    {
        public TransformingCtorEntity(string code, DateTimeOffset createdAt)
        {
            Code = code.ToUpperInvariant();
            CreatedAt = createdAt;
        }

        public string Code { get; set; } = default!;

        public DateTimeOffset CreatedAt { get; } = default!;
    }

    public class UncoveredReadOnlyLeafQuery
    {
        public IQueryable<UncoveredReadOnlyLeafEntity> GetItems(ISelection selection)
            =>
            new[]
            {
                new UncoveredReadOnlyLeafEntity
                {
                    Id = 1,
                    Nested = new UncoveredReadOnlyLeafNested { Name = "Nested" }
                }
            }
                .AsQueryable()
                .Select(selection.AsSelector<UncoveredReadOnlyLeafEntity>());
    }

    // Label is a read-only leaf that no constructor on this type covers (the only constructor
    // is parameterless), so it is dropped from the projection while the writable Id and the
    // nested navigation are still projected correctly.
    public sealed class UncoveredReadOnlyLeafEntity
    {
        public int Id { get; set; }

        public UncoveredReadOnlyLeafNested Nested { get; set; } = default!;

        public string? Label { get; }
    }

    public sealed class UncoveredReadOnlyLeafNested
    {
        public string Name { get; set; } = default!;
    }

    public class ComputedPropertyQuery
    {
        public IQueryable<ComputedPropertyEntity> GetItems(ISelection selection)
            => new[] { new ComputedPropertyEntity { Id = 1, Name = "foo" } }
                .AsQueryable()
                .Select(selection.AsSelector<ComputedPropertyEntity>());
    }

    // UpperName has a hand-written getter, so it is never eligible for projection and is always
    // skipped, regardless of selection.
    public sealed class ComputedPropertyEntity
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public string? UpperName => Name?.ToUpperInvariant();
    }

    public class PagingCtorQuery
    {
        [UsePaging]
        public IQueryable<PagingCtorEntity> GetItems(ISelection selection)
            =>
            new[]
            {
                new PagingCtorEntity("foo") { Id = 1 },
                new PagingCtorEntity("bar") { Id = 2 }
            }
                .AsQueryable()
                .Select(selection.AsSelector<PagingCtorEntity>());
    }

    // Id is writable and Name is a read-only leaf covered by the constructor. The private
    // parameterless constructor lets the "nodes" child selector build a MemberInit too, so
    // combining it with the "edges.node" selector merges two constructor-aware shapes instead
    // of merging an identity passthrough with a constructor call.
    public sealed class PagingCtorEntity
    {
        private PagingCtorEntity()
        {
        }

        public PagingCtorEntity(string name)
        {
            Name = name;
        }

        public int Id { get; set; }

        public string Name { get; } = default!;
    }

    public class Query
    {
        [UseProjection]
        public IQueryable<Tenant> GetTenants()
            => CreateTenants().AsQueryable();
    }

    public class AggregateCountryQuery
    {
        public IQueryable<AggregateCountry> GetCountries(ISelection selection)
            => new[] { new AggregateCountry("Foo", "FO", "FOO") }
                .AsQueryable()
                .Select(selection.AsSelector<AggregateCountry>());
    }

    public class AggregateRootBase<TId>
        where TId : struct
    {
        public TId Id { get; protected init; }
    }

    public sealed class AggregateCountry : AggregateRootBase<int>
    {
        private AggregateCountry()
        {
        }

        public AggregateCountry(string name, string alpha2Code, string alpha3Code)
        {
            Id = 1;
            Name = name;
            Alpha2Code = alpha2Code;
            Alpha3Code = alpha3Code;
        }

        // Read-only (get-only) properties, as used by DDD aggregates that set state through
        // the constructor. `CanWrite` is false, which is what caused these fields to be dropped
        // from the projection. The covering constructor is fed the source instance's own
        // property values, not the projected selection, so every argument (selected or not)
        // carries real data.
        public string Name { get; } = default!;

        public string Alpha2Code { get; } = default!;

        public string Alpha3Code { get; } = default!;
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
