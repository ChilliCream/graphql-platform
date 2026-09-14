# UseProjection_Should_Project_PerParent_When_FieldIsBatchResolved

## Result 1

```json
{
  "data": {
    "brands": [
      {
        "name": "Brand 1",
        "projectedProducts": [
          {
            "name": "Brand 1 P1"
          },
          {
            "name": "Brand 1 P2"
          }
        ]
      },
      {
        "name": "Brand 2",
        "projectedProducts": [
          {
            "name": "Brand 2 P1"
          },
          {
            "name": "Brand 2 P2"
          }
        ]
      }
    ]
  }
}
```

## Projected per-parent expressions

```json
[
  "<>z__ReadOnlyArray`1[HotChocolate.Types.BatchResolvers.ProjectionProduct].Select(_s1 => new ProjectionProduct() {Name = _s1.Name})",
  "<>z__ReadOnlyArray`1[HotChocolate.Types.BatchResolvers.ProjectionProduct].Select(_s1 => new ProjectionProduct() {Name = _s1.Name})"
]
```

## Captured SQL (root brands query)

```text
SELECT p."Name"
FROM "ProjectionBrands" AS p
```
