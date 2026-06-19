using GreenDonut.Data;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data;

public class ParentRequiresInterfaceProjectionTests
{
    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredColumn_When_ResolverDeclaredOnInterface()
    {
        // arrange
        // displayName is derived from Name, but the query selects only displayName; Name
        // must reach the projection via [Parent(requires:)], not through the selection set.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<Query>()
            .AddType<BrandInterfaceType>()
            .AddType<BrandType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              brands {
                displayName
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "displayName": "Acme"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredColumn_When_ResolverDeclaredOnObjectType()
    {
        // arrange
        // Same requirement, but declared directly on the object type (the path that already works).
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<ControlQuery>()
            .AddType<BrandControlType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              brands {
                displayName
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "displayName": "Acme"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredColumn_When_ObjectTypeRedeclaresInterfaceField()
    {
        // arrange
        // The object type declares displayName itself but without a resolver, so it inherits the
        // interface resolver through the field-merge path instead of receiving a copied field.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<Query>()
            .AddType<BrandInterfaceType>()
            .AddType<BrandRedeclaredType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              brands {
                displayName
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "displayName": "Acme"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task ParentRequires_Should_ProjectRequiredColumn_When_DeclaredViaFluentApiOnInterface()
    {
        // arrange
        // The requirement is declared through the fluent .ParentRequires<Brand>(...) on the interface
        // field rather than the [Parent(requires:)] attribute; Name must still reach the projection.
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<Query>()
            .AddType<BrandFluentInterfaceType>()
            .AddType<BrandFluentType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              brands {
                displayName
              }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "brands": [
                  {
                    "displayName": "Acme"
                  }
                ]
              }
            }
            """);
    }

    public interface IBrand
    {
        int Id { get; }
    }

    public class Brand : IBrand
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public class BrandResolvers
    {
        public string GetDisplayName([Parent(requires: nameof(Brand.Name))] Brand brand)
            => brand.Name;
    }

    public class BrandInterfaceType : InterfaceType<IBrand>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IBrand> descriptor)
        {
            descriptor
                .Field("displayName")
                .Type<NonNullType<StringType>>()
                .ResolveWith<BrandResolvers>(r => r.GetDisplayName(default!));
        }
    }

    public class BrandFluentResolvers
    {
        public string GetDisplayName([Parent] Brand brand)
            => brand.Name;
    }

    public class BrandFluentInterfaceType : InterfaceType<IBrand>
    {
        protected override void Configure(IInterfaceTypeDescriptor<IBrand> descriptor)
        {
            // The requirement is supplied via the fluent ParentRequires API, not the attribute.
            descriptor
                .Field("displayName")
                .Type<NonNullType<StringType>>()
                .ParentRequires<Brand>(nameof(Brand.Name))
                .ResolveWith<BrandFluentResolvers>(r => r.GetDisplayName(default!));
        }
    }

    public class BrandFluentType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor.Implements<BrandFluentInterfaceType>();
        }
    }

    public class BrandType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor.Implements<BrandInterfaceType>();
        }
    }

    public class BrandRedeclaredType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor.Implements<BrandInterfaceType>();

            // displayName is declared here without its own resolver; the interface resolver and its
            // [Parent(requires:)] requirement must be merged onto this already-declared field.
            descriptor
                .Field("displayName")
                .Type<NonNullType<StringType>>();
        }
    }

    public class Query
    {
        public IQueryable<Brand> GetBrands(QueryContext<Brand> query)
            => s_brands.AsQueryable().With(query);
    }

    public class BrandControlType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor
                .Field("displayName")
                .Type<NonNullType<StringType>>()
                .ResolveWith<BrandResolvers>(r => r.GetDisplayName(default!));
        }
    }

    public class ControlQuery
    {
        public IQueryable<Brand> GetBrands(QueryContext<Brand> query)
            => s_brands.AsQueryable().With(query);
    }

    private static readonly Brand[] s_brands =
    [
        new()
        {
            Id = 1,
            Name = "Acme"
        }
    ];
}
