# eShop Benchmark Subgraphs

This directory contains HotChocolate implementations of the GraphQL Hive benchmark subgraphs, ported from the [graphql-gateways-benchmark](https://github.com/graphql-hive/graphql-gateways-benchmark) repository.

## Subgraphs

### eShop.Accounts (Port 5001)
Manages user accounts and authentication.

**Types:**
- `User` - User account information with id, name, username, and birthday

**Queries:**
- `me` - Returns the current authenticated user
- `user(id: ID!)` - Get a specific user by ID
- `users` - Get all users

### eShop.Products (Port 5003)
Product catalog service.

**Types:**
- `Product` - Product information with UPC, name, price, and weight

**Queries:**
- `topProducts(first: Int = 5)` - Get the top N products (default 5)

### eShop.Inventory (Port 5002)
Inventory and shipping estimation service. Extends the Product type.

**Extended Fields on Product:**
- `inStock` - Boolean indicating if product is in stock
- `shippingEstimate` - Calculated shipping estimate based on price and weight

### eShop.Reviews (Port 5004)
Product review service. Extends both User and Product types.

**Types:**
- `Review` - Review with id, body, author, and product

**Extended Fields:**
- `User.reviews` - List of reviews by the user
- `Product.reviews` - List of reviews for the product

## Running the Subgraphs

Each subgraph is a standalone ASP.NET Core application. Run them individually:

```bash
# Terminal 1 - Accounts
cd eShop.Accounts
dotnet run

# Terminal 2 - Products
cd eShop.Products
dotnet run

# Terminal 3 - Inventory
cd eShop.Inventory
dotnet run

# Terminal 4 - Reviews
cd eShop.Reviews
dotnet run
```

## GraphQL Endpoints

Once running, each subgraph exposes a GraphQL endpoint at:

- Accounts: `http://localhost:5001/graphql`
- Inventory: `http://localhost:5002/graphql`
- Products: `http://localhost:5003/graphql`
- Reviews: `http://localhost:5004/graphql`

## Federation

All subgraphs are configured with Apollo Federation support and publish their schema definitions. They can be composed into a federated gateway for benchmarking.

## Sample Data

Each subgraph includes hardcoded sample data matching the original Rust implementations:
- 6 users
- 9 products
- 11 reviews
- Inventory status for all products

## Architecture Notes

These implementations use:
- **.NET 9.0** target framework
- **HotChocolate 15.1.1** GraphQL server
- **Apollo Federation** for schema composition
- **Minimal APIs** with top-level statements
- **In-memory data** for benchmarking purposes
