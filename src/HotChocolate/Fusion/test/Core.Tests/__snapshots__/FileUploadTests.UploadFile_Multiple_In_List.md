# UploadFile_Multiple_In_List

## Result

```json
{
  "data": {
    "uploadMultipleProductPictures": {
      "boolean": true
    }
  }
}
```

## Request

```graphql
mutation UploadMultiple($input: UploadMultipleProductPicturesInput!) {
  uploadMultipleProductPictures(input: $input) {
    boolean
  }
}
```

## QueryPlan Hash

```text
EECC36ABA7A36C87BBC6C16A429568EAF4DC5583
```

## QueryPlan

```json
{
  "document": "mutation UploadMultiple($input: UploadMultipleProductPicturesInput!) { uploadMultipleProductPictures(input: $input) { boolean } }",
  "operation": "UploadMultiple",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "mutation UploadMultiple_1($input: UploadMultipleProductPicturesInput!) { uploadMultipleProductPictures(input: $input) { boolean } }",
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

