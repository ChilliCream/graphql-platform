# Error_Union_With_Inline_Fragment

## Result

```json
{
  "data": {
    "uploadProductPicture": {
      "boolean": true,
      "errors": null
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
      ... on ProductNotFoundError {
        productId
      }
    }
  }
}
```

## QueryPlan Hash

```text
62AB3FC1A9F040C10377EAF01865D8D8CB406CB4
```

## QueryPlan

```json
{
  "document": "mutation Upload($input: UploadProductPictureInput!) { uploadProductPicture(input: $input) { boolean errors { __typename ... on ProductNotFoundError { productId } } } }",
  "operation": "Upload",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Resolve",
        "subgraph": "Products",
        "document": "mutation Upload_1($input: UploadProductPictureInput!) { uploadProductPicture(input: $input) { boolean errors { __typename ... on ProductNotFoundError { __typename productId } } } }",
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

