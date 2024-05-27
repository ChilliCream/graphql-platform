# UploadFile_2

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
mutation Upload($input: UploadProductPictureInput!) {
  uploadProductPicture(input: $input) {
    boolean
  }
}
```

## QueryPlan Hash

```text
D968748DEC5B900198E5ABEBE187BCF83431A5AC
```

## QueryPlan

```json
{
  "document": "mutation Upload($input: UploadProductPictureInput!) { uploadProductPicture(input: $input) { boolean } }",
  "operation": "Upload",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "mutation Upload_1($input: UploadProductPictureInput!) { uploadProductPicture(input: $input) { boolean } }",
        "selectionSetId": 0,
        "forwardedVariables": [
          {
            "variable": "input"
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

