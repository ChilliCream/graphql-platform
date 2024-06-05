# Error_Union_With_TypeName_Errors_Not_Null

## Result

```json
{
  "data": {
    "uploadProductPicture": {
      "boolean": null,
      "errors": [
        {
          "__typename": "ProductNotFoundError"
        }
      ]
    }
  }
}
```

## Request

```graphql
mutation Upload($input: UploadProductPictureInput!) {
  uploadProductPicture(input: $input) {
    boolean
    errors {
      __typename
    }
  }
}
```

## QueryPlan Hash

```text
56E8F115508796D7CB6FCA52CE2AFFCF458ED8E0
```

## QueryPlan

```json
{
  "document": "mutation Upload($input: UploadProductPictureInput!) { uploadProductPicture(input: $input) { boolean errors { __typename } } }",
  "operation": "Upload",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "mutation Upload_1($input: UploadProductPictureInput!) { uploadProductPicture(input: $input) { boolean errors { __typename } } }",
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

