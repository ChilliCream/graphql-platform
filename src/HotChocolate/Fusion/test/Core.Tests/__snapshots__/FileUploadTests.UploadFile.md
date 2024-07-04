# UploadFile

## Result

```json
{
  "data": {
    "uploadProductPicture": {
      "boolean": true
    }
  }
}
```

## Request

```graphql
mutation Upload($file: Upload!) {
  uploadProductPicture(input: { productId: 1, file: $file }) {
    boolean
  }
}
```

## QueryPlan Hash

```text
C3298260FD8612E0FE8DB108E15FB4122CBBF47C
```

## QueryPlan

```json
{
  "document": "mutation Upload($file: Upload!) { uploadProductPicture(input: { productId: 1, file: $file }) { boolean } }",
  "operation": "Upload",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "mutation Upload_1($file: Upload!) { uploadProductPicture(input: { productId: 1, file: $file }) { boolean } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "file"
          }
        ]
      },
      {
        "type": "Compose",
        "selectionSetIds": [
          0
        ]
      }
    ]
  }
}
```

