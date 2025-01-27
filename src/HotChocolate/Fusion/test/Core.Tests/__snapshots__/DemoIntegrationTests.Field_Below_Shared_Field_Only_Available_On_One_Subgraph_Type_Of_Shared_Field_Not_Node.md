# Field_Below_Shared_Field_Only_Available_On_One_Subgraph_Type_Of_Shared_Field_Not_Node

## Result

```json
{
  "errors": [
    {
      "message": "Unexpected Execution Error"
    }
  ]
}
```

## Request

```graphql
query($productId: ID!) {
  productById(id: $productId) {
    subgraph1Only {
      shared {
        subgraph2Only
      }
    }
  }
}
```

