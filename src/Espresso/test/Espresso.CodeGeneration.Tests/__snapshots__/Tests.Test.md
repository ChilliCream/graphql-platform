# Test

## GetProductByIdQuery.g.cs

```csharp
namespace Espresso.CodeGeneration

public record GetProductByIdQuery
{
    public required  productById { get; init; }
}

```

## GetProductByIdQuery_productById.g.cs

```csharp
namespace Espresso.CodeGeneration

public record GetProductByIdQuery_productById
{
    public required  id { get; init; }
    public required  name { get; init; }
    public required  price { get; init; }
}

```

