# UseProjection_Should_ComposeQuery_When_BatchReturnsQueryable

## Result

```json
{
  "data": {
    "brands": [
      {
        "products": [
          {
            "name": "1-B"
          },
          {
            "name": "1-A"
          }
        ]
      },
      {
        "products": [
          {
            "name": "2-B"
          },
          {
            "name": "2-A"
          }
        ]
      }
    ]
  }
}
```

## Resolver batches

```json
[
  [
    1,
    2
  ]
]
```

## Composed queries

```json
[
  "HotChocolate.Data.Projections.ProjectionBatchResolverInteractionTests+ProjectedProduct[].OrderByDescending(x => IIF((x == null), default(String), x.Name)).Where(_s0 => (((_s0 != null) AndAlso ((_s0.Name != null) AndAlso _s0.Name.EndsWith(ExpressionParameter { p = A }.p))) OrElse ((_s0 != null) AndAlso ((_s0.Name != null) AndAlso _s0.Name.EndsWith(ExpressionParameter { p = B }.p))))).Select(_s1 => new ProjectedProduct() {Name = _s1.Name})",
  "HotChocolate.Data.Projections.ProjectionBatchResolverInteractionTests+ProjectedProduct[].OrderByDescending(x => IIF((x == null), default(String), x.Name)).Where(_s0 => (((_s0 != null) AndAlso ((_s0.Name != null) AndAlso _s0.Name.EndsWith(ExpressionParameter { p = A }.p))) OrElse ((_s0 != null) AndAlso ((_s0.Name != null) AndAlso _s0.Name.EndsWith(ExpressionParameter { p = B }.p))))).Select(_s1 => new ProjectedProduct() {Name = _s1.Name})"
]
```

## Projected values before completion

```json
[
  "1-B:",
  "1-A:",
  "2-B:",
  "2-A:"
]
```
