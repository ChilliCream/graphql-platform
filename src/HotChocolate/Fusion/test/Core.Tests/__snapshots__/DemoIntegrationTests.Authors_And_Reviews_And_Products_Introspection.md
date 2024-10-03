# Authors_And_Reviews_And_Products_Introspection

## Result

```json
{
  "data": {
    "__schema": {
      "types": [
        {
          "name": "__Directive",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "name",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "description",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "locations",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "args",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "isRepeatable",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "__DirectiveLocation",
          "kind": "ENUM",
          "fields": null
        },
        {
          "name": "__EnumValue",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "name",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "description",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "isDeprecated",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "deprecationReason",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            }
          ]
        },
        {
          "name": "__Field",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "name",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "description",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "args",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "type",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "isDeprecated",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "deprecationReason",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            }
          ]
        },
        {
          "name": "__InputValue",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "name",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "description",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "type",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "defaultValue",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "isDeprecated",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "deprecationReason",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            }
          ]
        },
        {
          "name": "__Schema",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "description",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "types",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "queryType",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "mutationType",
              "type": {
                "name": "__Type",
                "kind": "OBJECT"
              }
            },
            {
              "name": "subscriptionType",
              "type": {
                "name": "__Type",
                "kind": "OBJECT"
              }
            },
            {
              "name": "directives",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "__Type",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "kind",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "name",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "description",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "fields",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            },
            {
              "name": "interfaces",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            },
            {
              "name": "possibleTypes",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            },
            {
              "name": "enumValues",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            },
            {
              "name": "inputFields",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            },
            {
              "name": "ofType",
              "type": {
                "name": "__Type",
                "kind": "OBJECT"
              }
            },
            {
              "name": "specifiedByURL",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "oneOf",
              "type": {
                "name": "Boolean",
                "kind": "SCALAR"
              }
            }
          ]
        },
        {
          "name": "__TypeKind",
          "kind": "ENUM",
          "fields": null
        },
        {
          "name": "Upload",
          "kind": "SCALAR",
          "fields": null
        },
        {
          "name": "Query",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "errorField",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "productBookmarkByUsername",
              "type": {
                "name": "ProductBookmark",
                "kind": "OBJECT"
              }
            },
            {
              "name": "productById",
              "type": {
                "name": "Product",
                "kind": "OBJECT"
              }
            },
            {
              "name": "productConfigurationByUsername",
              "type": {
                "name": "ProductConfiguration",
                "kind": "OBJECT"
              }
            },
            {
              "name": "reviewById",
              "type": {
                "name": "Review",
                "kind": "OBJECT"
              }
            },
            {
              "name": "reviewOrAuthor",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "reviews",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "topProducts",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "userById",
              "type": {
                "name": "User",
                "kind": "OBJECT"
              }
            },
            {
              "name": "users",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "usersById",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "viewer",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "Mutation",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "addReview",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "addUser",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "uploadMultipleProductPictures",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "uploadProductPicture",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "Subscription",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "onNewReview",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "AddReviewPayload",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "review",
              "type": {
                "name": "Review",
                "kind": "OBJECT"
              }
            }
          ]
        },
        {
          "name": "AddUserPayload",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "user",
              "type": {
                "name": "User",
                "kind": "OBJECT"
              }
            }
          ]
        },
        {
          "name": "Product",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "dimension",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "id",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "name",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "price",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "repeat",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "repeatData",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "reviews",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "weight",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "ProductBookmark",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "id",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "note",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "productId",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "username",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "ProductConfiguration",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "configurationName",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "id",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "productId",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "username",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "ProductDimension",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "size",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "weight",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "ProductNotFoundError",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "message",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "productId",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "Review",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "author",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "body",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "errorField",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "id",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "product",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "SomeData",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "accountValue",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "data",
              "type": {
                "name": "SomeData",
                "kind": "OBJECT"
              }
            },
            {
              "name": "num",
              "type": {
                "name": "Int",
                "kind": "SCALAR"
              }
            },
            {
              "name": "reviewsValue",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "UploadMultipleProductPicturesPayload",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "boolean",
              "type": {
                "name": "Boolean",
                "kind": "SCALAR"
              }
            },
            {
              "name": "errors",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            }
          ]
        },
        {
          "name": "UploadProductPicturePayload",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "boolean",
              "type": {
                "name": "Boolean",
                "kind": "SCALAR"
              }
            },
            {
              "name": "errors",
              "type": {
                "name": null,
                "kind": "LIST"
              }
            }
          ]
        },
        {
          "name": "User",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "birthdate",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "errorField",
              "type": {
                "name": "String",
                "kind": "SCALAR"
              }
            },
            {
              "name": "id",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "name",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "productBookmarkByUsername",
              "type": {
                "name": "ProductBookmark",
                "kind": "OBJECT"
              }
            },
            {
              "name": "productConfigurationByUsername",
              "type": {
                "name": "ProductConfiguration",
                "kind": "OBJECT"
              }
            },
            {
              "name": "reviews",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "username",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "Viewer",
          "kind": "OBJECT",
          "fields": [
            {
              "name": "data",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            },
            {
              "name": "latestReview",
              "type": {
                "name": "Review",
                "kind": "OBJECT"
              }
            },
            {
              "name": "user",
              "type": {
                "name": "User",
                "kind": "OBJECT"
              }
            }
          ]
        },
        {
          "name": "Error",
          "kind": "INTERFACE",
          "fields": [
            {
              "name": "message",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "Node",
          "kind": "INTERFACE",
          "fields": [
            {
              "name": "id",
              "type": {
                "name": null,
                "kind": "NON_NULL"
              }
            }
          ]
        },
        {
          "name": "ReviewOrAuthor",
          "kind": "UNION",
          "fields": null
        },
        {
          "name": "UploadMultipleProductPicturesError",
          "kind": "UNION",
          "fields": null
        },
        {
          "name": "UploadProductPictureError",
          "kind": "UNION",
          "fields": null
        },
        {
          "name": "AddReviewInput",
          "kind": "INPUT_OBJECT",
          "fields": null
        },
        {
          "name": "AddUserInput",
          "kind": "INPUT_OBJECT",
          "fields": null
        },
        {
          "name": "ProductIdWithUploadInput",
          "kind": "INPUT_OBJECT",
          "fields": null
        },
        {
          "name": "SomeDataInput",
          "kind": "INPUT_OBJECT",
          "fields": null
        },
        {
          "name": "UploadMultipleProductPicturesInput",
          "kind": "INPUT_OBJECT",
          "fields": null
        },
        {
          "name": "UploadProductPictureInput",
          "kind": "INPUT_OBJECT",
          "fields": null
        },
        {
          "name": "String",
          "kind": "SCALAR",
          "fields": null
        },
        {
          "name": "Boolean",
          "kind": "SCALAR",
          "fields": null
        },
        {
          "name": "ID",
          "kind": "SCALAR",
          "fields": null
        },
        {
          "name": "Int",
          "kind": "SCALAR",
          "fields": null
        },
        {
          "name": "Date",
          "kind": "SCALAR",
          "fields": null
        }
      ]
    }
  }
}
```

## Request

```graphql
query Introspect {
  __schema {
    types {
      name
      kind
      fields {
        name
        type {
          name
          kind
        }
      }
    }
  }
}
```

## QueryPlan Hash

```text
3F4377696891DA698EAFD7B8D8BD8CB60FEC342E
```

## QueryPlan

```json
{
  "document": "query Introspect { __schema { types { name kind fields { name type { name kind } } } } }",
  "operation": "Introspect",
  "rootNode": {
    "type": "Sequence",
    "nodes": [
      {
        "type": "Introspect",
        "document": "{ __schema { types { name kind fields { name type { name kind } } } } }"
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

