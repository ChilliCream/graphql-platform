using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddFilteringSortingDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "filtering-basic",
                Title = "Filtering with UseFiltering",
                Category = BestPracticeCategory.FilteringSorting,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "filter where search query criteria condition IQueryable dynamic narrow results",
                Abstract =
                    "How to add filtering to queries using [UseFiltering] and EF Core integration. Covers filter convention configuration.",
                Body = """
                # Filtering with UseFiltering

                ## When to Use

                Use `[UseFiltering]` when you want to allow clients to filter query results dynamically. Hot Chocolate's filtering generates strongly-typed filter input types from your C# model, translating client filter expressions into `IQueryable<T>` `Where` clauses that execute at the database level.

                This is appropriate for:
                - List queries where clients need to narrow results by various criteria
                - Admin interfaces with dynamic search capabilities
                - Any query returning `IQueryable<T>` where database-level filtering is desired

                ## Implementation

                ### Basic Filtering

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products;
                }
                ```

                ### Generated Filter Types

                For a `Product` class:

                ```csharp
                public class Product
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;
                    public decimal Price { get; set; }
                    public bool IsActive { get; set; }
                    public DateTime CreatedAt { get; set; }
                    public int CategoryId { get; set; }
                }
                ```

                Hot Chocolate generates:

                ```graphql
                input ProductFilterInput {
                  and: [ProductFilterInput!]
                  or: [ProductFilterInput!]
                  id: IntOperationFilterInput
                  name: StringOperationFilterInput
                  price: DecimalOperationFilterInput
                  isActive: BooleanOperationFilterInput
                  createdAt: DateTimeOperationFilterInput
                  categoryId: IntOperationFilterInput
                }

                input StringOperationFilterInput {
                  eq: String
                  neq: String
                  contains: String
                  ncontains: String
                  startsWith: String
                  nstartsWith: String
                  endsWith: String
                  nendsWith: String
                  in: [String]
                  nin: [String]
                }
                ```

                ### Client Queries

                ```graphql
                # Filter active products with price > 10
                query {
                  products(
                    where: {
                      isActive: { eq: true }
                      price: { gt: 10 }
                    }
                  ) {
                    nodes {
                      id
                      name
                      price
                    }
                  }
                }

                # Complex filter with OR
                query {
                  products(
                    where: {
                      or: [
                        { name: { contains: "Widget" } }
                        { price: { lte: 5 } }
                      ]
                    }
                  ) {
                    nodes {
                      id
                      name
                    }
                  }
                }
                ```

                ### Configuring Allowed Filters

                Restrict which fields can be filtered:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddFiltering(f => f
                        .AddDefaults()
                        .BindRuntimeType<Product>(descriptor =>
                        {
                            descriptor.Field(p => p.Name);
                            descriptor.Field(p => p.Price);
                            descriptor.Field(p => p.IsActive);
                            // Id, CreatedAt, CategoryId are not filterable
                        }));
                ```

                ### Adding Filtering to Existing Service

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddFiltering()
                    .AddSorting()
                    .RegisterDbContextFactory<AppDbContext>();
                ```

                ## Anti-patterns

                **Applying filtering to materialized collections:**

                ```csharp
                // BAD: Loads all products into memory, then filters
                [UseFiltering]
                public static async Task<List<Product>> GetProducts(AppDbContext dbContext)
                {
                    return await dbContext.Products.ToListAsync(); // All in memory!
                }
                ```

                **Exposing sensitive fields to filtering:**

                ```csharp
                // BAD: Allowing clients to filter by internal fields like PasswordHash
                public class User
                {
                    public int Id { get; set; }
                    public string Email { get; set; } = default!;
                    public string PasswordHash { get; set; } = default!; // Filterable!
                }
                // Use [GraphQLIgnore] or filter configuration to exclude sensitive fields
                ```

                **Not combining with pagination:**

                ```csharp
                // BAD: Filtering without pagination can return unbounded result sets
                [UseFiltering]
                public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                    => dbContext.Products; // Could return millions of rows!
                // Always combine with [UsePaging] or [UseOffsetPaging]
                ```

                ## Key Points

                - Use `[UseFiltering]` on `IQueryable<T>` returning resolvers for database-level filtering
                - Always combine filtering with pagination to prevent unbounded result sets
                - Filter input types are auto-generated from C# model properties
                - Use filter conventions to restrict which fields and operations are available
                - Attribute order matters: `[UsePaging]` before `[UseFiltering]` before `[UseSorting]`
                - Use `[GraphQLIgnore]` on model properties to exclude them from filter generation

                ## Related Practices

                - [sorting-basic] — For sorting alongside filtering
                - [pagination-cursor] — For cursor pagination with filtering
                - [filtering-custom] — For custom filter types
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "filtering-custom",
                Title = "Custom Filter Types and Operations",
                Category = BestPracticeCategory.FilteringSorting,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "custom filter convention operator handler predicate restrict whitelist",
                Abstract =
                    "How to implement custom filter input types, custom filter handlers, and extend the default filter convention with domain-specific operations.",
                Body = """
                # Custom Filter Types and Operations

                ## When to Use

                Use custom filter types when the auto-generated filters do not meet your requirements. Common scenarios include:

                - Adding computed or derived filter fields that do not map to model properties
                - Restricting operations on specific fields (e.g., only allow `eq` on status fields)
                - Adding full-text search integration
                - Creating filters over navigated properties or complex expressions

                ## Implementation

                ### Custom Filter Input Type

                Create a custom filter type to control exactly which fields and operations are available:

                ```csharp
                namespace MyApp.GraphQL.Filters;

                public class ProductFilterType : FilterInputType<Product>
                {
                    protected override void Configure(IFilterInputTypeDescriptor<Product> descriptor)
                    {
                        descriptor.BindFieldsExplicitly();

                        descriptor.Field(p => p.Name)
                            .Type<StringOperationFilterInputType>();

                        descriptor.Field(p => p.Price)
                            .Type<DecimalOperationFilterInputType>();

                        descriptor.Field(p => p.IsActive)
                            .Type<BooleanOperationFilterInputType>();

                        descriptor.Field(p => p.CreatedAt)
                            .Type<DateTimeOperationFilterInputType>();
                    }
                }
                ```

                Register the custom filter type:

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseFiltering(typeof(ProductFilterType))]
                    [UseSorting]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products;
                }
                ```

                ### Restricting Operations on Fields

                ```csharp
                public class OrderFilterType : FilterInputType<Order>
                {
                    protected override void Configure(IFilterInputTypeDescriptor<Order> descriptor)
                    {
                        descriptor.BindFieldsExplicitly();

                        // Only allow equality check on status
                        descriptor.Field(o => o.Status)
                            .Type<EnumOperationFilterInputType<OrderStatus>>();

                        // Allow range operations on dates
                        descriptor.Field(o => o.CreatedAt)
                            .Type<DateTimeOperationFilterInputType>();

                        // Allow contains/startsWith on customer name
                        descriptor.Field(o => o.CustomerName)
                            .Type<StringOperationFilterInputType>();
                    }
                }
                ```

                ### Filtering on Nested Properties

                ```csharp
                public class OrderFilterType : FilterInputType<Order>
                {
                    protected override void Configure(IFilterInputTypeDescriptor<Order> descriptor)
                    {
                        descriptor.Field(o => o.Customer)
                            .Type<CustomerFilterType>();
                    }
                }

                public class CustomerFilterType : FilterInputType<Customer>
                {
                    protected override void Configure(IFilterInputTypeDescriptor<Customer> descriptor)
                    {
                        descriptor.BindFieldsExplicitly();
                        descriptor.Field(c => c.Name);
                        descriptor.Field(c => c.Email);
                    }
                }
                ```

                ### Global Filter Conventions

                Configure conventions that apply to all filter types:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddFiltering(c => c
                        .AddDefaults()
                        .Provider(new QueryableFilterProvider(p => p
                            .AddDefaultFieldHandlers())));
                ```

                ## Anti-patterns

                **Exposing all operations on all fields:**

                ```csharp
                // BAD: Allowing 'contains' on ID fields or 'gt/lt' on boolean fields
                // makes no semantic sense and increases attack surface
                [UseFiltering] // Default: all operations on all fields
                public static IQueryable<User> GetUsers(AppDbContext dbContext)
                    => dbContext.Users;
                // Use custom filter types to restrict operations
                ```

                **Complex filters without query cost analysis:**

                ```csharp
                // BAD: Deeply nested OR conditions can generate expensive SQL
                query {
                  products(where: {
                    or: [
                      { name: { contains: "a" } }
                      { name: { contains: "b" } }
                      # ... 50 more OR conditions
                    ]
                  }) { nodes { id } }
                }
                // Combine with query complexity limits
                ```

                **Filtering on computed properties:**

                ```csharp
                // BAD: Properties not backed by database columns cannot be translated to SQL
                public class Product
                {
                    public decimal Price { get; set; }
                    public decimal Tax { get; set; }

                    [NotMapped]
                    public decimal TotalPrice => Price + Tax; // Not in database!
                }
                // Filtering on TotalPrice causes client-side evaluation or errors
                ```

                ## Key Points

                - Use `FilterInputType<T>` to create custom filter types with explicit field and operation control
                - Call `BindFieldsExplicitly()` to only expose the fields you explicitly configure
                - Pass custom filter types via `[UseFiltering(typeof(MyFilterType))]`
                - Filter on database-backed columns only — computed or `[NotMapped]` properties cause issues
                - Restrict operations to what makes sense semantically (e.g., no `contains` on numeric fields)
                - Combine custom filters with query complexity limits to prevent expensive queries

                ## Related Practices

                - [filtering-basic] — For default filtering setup
                - [sorting-custom] — For custom sort types
                - [security-production-hardening] — For query complexity limits
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "sorting-basic",
                Title = "Sorting with UseSorting",
                Category = BestPracticeCategory.FilteringSorting,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "sort order orderby ascending descending arrange rank organize",
                Abstract =
                    "How to add sorting to queries using [UseSorting] and EF Core integration. Covers sort convention configuration.",
                Body = """
                # Sorting with UseSorting

                ## When to Use

                Use `[UseSorting]` when clients need to control the sort order of query results. Hot Chocolate generates strongly-typed sort input types from your C# model, translating sort expressions into `IQueryable<T>` `OrderBy` clauses at the database level.

                Sorting is typically used alongside filtering and pagination for list queries.

                ## Implementation

                ### Basic Sorting

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products;
                }
                ```

                ### Generated Sort Types

                For a `Product` class:

                ```csharp
                public class Product
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;
                    public decimal Price { get; set; }
                    public DateTime CreatedAt { get; set; }
                }
                ```

                Hot Chocolate generates:

                ```graphql
                input ProductSortInput {
                  id: SortEnumType
                  name: SortEnumType
                  price: SortEnumType
                  createdAt: SortEnumType
                }

                enum SortEnumType {
                  ASC
                  DESC
                }
                ```

                ### Client Queries

                ```graphql
                # Sort by price ascending
                query {
                  products(order: [{ price: ASC }]) {
                    nodes {
                      id
                      name
                      price
                    }
                  }
                }

                # Multi-column sort: by category then by name
                query {
                  products(order: [{ createdAt: DESC }, { name: ASC }]) {
                    nodes {
                      id
                      name
                      createdAt
                    }
                  }
                }
                ```

                ### Default Sort Order

                Provide a default sort when clients do not specify one:

                ```csharp
                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseFiltering]
                    [UseSorting]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products.OrderByDescending(p => p.CreatedAt);
                }
                ```

                ### Restricting Sortable Fields

                ```csharp
                public class ProductSortType : SortInputType<Product>
                {
                    protected override void Configure(ISortInputTypeDescriptor<Product> descriptor)
                    {
                        descriptor.BindFieldsExplicitly();
                        descriptor.Field(p => p.Name);
                        descriptor.Field(p => p.Price);
                        descriptor.Field(p => p.CreatedAt);
                        // Id is not sortable
                    }
                }

                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    [UseSorting(typeof(ProductSortType))]
                    public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                        => dbContext.Products;
                }
                ```

                ## Anti-patterns

                **Sorting in memory instead of at the database:**

                ```csharp
                // BAD: Loads all products into memory, then sorts
                [UseSorting]
                public static async Task<List<Product>> GetProducts(AppDbContext dbContext)
                {
                    return await dbContext.Products.ToListAsync();
                }
                ```

                **Allowing sorting on non-indexed columns:**

                ```csharp
                // BAD: Sorting by Description (typically a TEXT column without an index)
                // causes full table scans on large datasets
                public class Product
                {
                    public int Id { get; set; }
                    public string Description { get; set; } = default!; // Long text, no index
                }
                // Restrict sortable fields to indexed columns
                ```

                **No default sort order with pagination:**

                ```csharp
                // BAD: Without a default sort, pagination is non-deterministic
                [UsePaging]
                [UseSorting]
                public static IQueryable<Product> GetProducts(AppDbContext dbContext)
                    => dbContext.Products; // If client does not sort, order is undefined
                ```

                ## Key Points

                - Use `[UseSorting]` on `IQueryable<T>` return types for database-level sorting
                - Sort input types are auto-generated from C# model properties
                - Clients can specify multi-column sort using an array: `order: [{price: ASC}, {name: ASC}]`
                - Always provide a default sort order as a fallback when the client does not specify one
                - Use custom `SortInputType<T>` to restrict which fields are sortable
                - Only expose sorting on indexed database columns for good performance

                ## Related Practices

                - [filtering-basic] — For filtering alongside sorting
                - [sorting-custom] — For custom sort types
                - [pagination-cursor] — For combining sorting with pagination
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "sorting-custom",
                Title = "Custom Sort Types",
                Category = BestPracticeCategory.FilteringSorting,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "custom sort convention sort handler sort operation preset named",
                Abstract =
                    "How to implement custom sort input types and extend the sort convention with computed sorts or multi-column sort expressions.",
                Body = """
                # Custom Sort Types

                ## When to Use

                Use custom sort types when the auto-generated sort input types do not meet your requirements. Common scenarios include:

                - Restricting which fields are sortable
                - Adding sort options for nested/navigated properties
                - Creating named sort presets for common orderings
                - Sorting on computed database expressions

                ## Implementation

                ### Custom Sort Input Type

                ```csharp
                namespace MyApp.GraphQL.Sorting;

                public class OrderSortType : SortInputType<Order>
                {
                    protected override void Configure(ISortInputTypeDescriptor<Order> descriptor)
                    {
                        descriptor.BindFieldsExplicitly();

                        descriptor.Field(o => o.CreatedAt)
                            .Name("createdAt");

                        descriptor.Field(o => o.TotalAmount)
                            .Name("totalAmount");

                        descriptor.Field(o => o.Status)
                            .Name("status");

                        // Sort by nested property
                        descriptor.Field(o => o.Customer)
                            .Type<CustomerSortType>();
                    }
                }

                public class CustomerSortType : SortInputType<Customer>
                {
                    protected override void Configure(ISortInputTypeDescriptor<Customer> descriptor)
                    {
                        descriptor.BindFieldsExplicitly();
                        descriptor.Field(c => c.Name);
                    }
                }
                ```

                Register and use:

                ```csharp
                [QueryType]
                public static class OrderQueries
                {
                    [UsePaging]
                    [UseFiltering]
                    [UseSorting(typeof(OrderSortType))]
                    public static IQueryable<Order> GetOrders(AppDbContext dbContext)
                        => dbContext.Orders;
                }
                ```

                ### Client Query with Nested Sort

                ```graphql
                query {
                  orders(order: [{ customer: { name: ASC } }, { createdAt: DESC }]) {
                    nodes {
                      id
                      totalAmount
                      customer {
                        name
                      }
                    }
                  }
                }
                ```

                ### Default Sort Convention

                Configure sort conventions globally:

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddSorting(c => c
                        .AddDefaults()
                        .Provider(new QueryableSortProvider(p => p
                            .AddDefaultFieldHandlers())));
                ```

                ### Enumeration Sort (Named Presets)

                For simpler APIs, expose named sort options as an enum:

                ```csharp
                public enum ProductSortOrder
                {
                    [GraphQLDescription("Sort by name A-Z")]
                    NameAsc,

                    [GraphQLDescription("Sort by name Z-A")]
                    NameDesc,

                    [GraphQLDescription("Sort by price low to high")]
                    PriceLowToHigh,

                    [GraphQLDescription("Sort by price high to low")]
                    PriceHighToLow,

                    [GraphQLDescription("Sort by newest first")]
                    Newest
                }

                [QueryType]
                public static class ProductQueries
                {
                    [UsePaging]
                    public static IQueryable<Product> GetProducts(
                        AppDbContext dbContext,
                        ProductSortOrder sortBy = ProductSortOrder.Newest)
                    {
                        return sortBy switch
                        {
                            ProductSortOrder.NameAsc => dbContext.Products.OrderBy(p => p.Name),
                            ProductSortOrder.NameDesc => dbContext.Products.OrderByDescending(p => p.Name),
                            ProductSortOrder.PriceLowToHigh => dbContext.Products.OrderBy(p => p.Price),
                            ProductSortOrder.PriceHighToLow => dbContext.Products.OrderByDescending(p => p.Price),
                            ProductSortOrder.Newest => dbContext.Products.OrderByDescending(p => p.CreatedAt),
                            _ => dbContext.Products.OrderByDescending(p => p.CreatedAt)
                        };
                    }
                }
                ```

                ## Anti-patterns

                **Sorting on non-mapped computed properties:**

                ```csharp
                // BAD: Computed properties cannot be translated to SQL
                public class Product
                {
                    public decimal Price { get; set; }
                    public decimal Discount { get; set; }

                    [NotMapped]
                    public decimal EffectivePrice => Price - Discount;
                }
                // Sorting on EffectivePrice fails at the database level
                ```

                **Allowing unlimited sort depth on nested properties:**

                ```csharp
                // BAD: Deep nested sorts generate complex JOINs
                // order: [{ customer: { address: { city: { country: { name: ASC } } } } }]
                // Restrict sort depth with explicit field binding
                ```

                **Exposing all properties without filtering:**

                ```csharp
                // BAD: Auto-generated sorts on all properties including large text columns
                public class Article
                {
                    public int Id { get; set; }
                    public string Title { get; set; } = default!;
                    public string Content { get; set; } = default!; // Sorting on text is slow
                }
                ```

                ## Key Points

                - Use `SortInputType<T>` with `BindFieldsExplicitly()` to control sortable fields
                - Use `[UseSorting(typeof(MySortType))]` to apply custom sort types
                - Nested property sorts require explicit `SortInputType` definitions for each level
                - Consider enum-based sort presets for simpler client APIs with predefined orderings
                - Only expose sorting on indexed columns for acceptable query performance
                - Restrict sort depth on nested properties to prevent expensive JOIN operations

                ## Related Practices

                - [sorting-basic] — For default sorting setup
                - [filtering-custom] — For custom filter types
                - [pagination-cursor] — For combining sorting with pagination
                """
            });
    }
}
