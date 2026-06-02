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
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<Query>()
            .AddType<BrandInterfaceType>()
            .AddType<BrandType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        // displayName is derived from Name, but the query selects only displayName; Name
        // must reach the projection via [Parent(requires:)], not through the selection set.
        var result = await executor.ExecuteAsync(
            """
            {
              brands {
                displayName
              }
            }
            """);

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
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<ControlQuery>()
            .AddType<BrandControlType>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync();

        // Same requirement, but declared directly on the object type (the path that already works).
        var result = await executor.ExecuteAsync(
            """
            {
              brands {
                displayName
              }
            }
            """);

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

    public class BrandType : ObjectType<Brand>
    {
        protected override void Configure(IObjectTypeDescriptor<Brand> descriptor)
        {
            descriptor.Implements<BrandInterfaceType>();
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
