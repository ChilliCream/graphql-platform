# Nested_Paging_First_2

```json
{
  "data": {
    "brands": {
      "edges": [
        {
          "cursor": "QnJhbmQwOjE="
        },
        {
          "cursor": "QnJhbmQxOjI="
        }
      ],
      "nodes": [
        {
          "products": {
            "nodes": [
              {
                "name": "Product 0-0"
              },
              {
                "name": "Product 0-1"
              }
            ],
            "pageInfo": {
              "hasNextPage": true,
              "hasPreviousPage": false,
              "startCursor": "UHJvZHVjdCAwLTA6MQ==",
              "endCursor": "UHJvZHVjdCAwLTE6Mg=="
            }
          }
        },
        {
          "products": {
            "nodes": [
              {
                "name": "Product 1-0"
              },
              {
                "name": "Product 1-1"
              }
            ],
            "pageInfo": {
              "hasNextPage": true,
              "hasPreviousPage": false,
              "startCursor": "UHJvZHVjdCAxLTA6MTAx",
              "endCursor": "UHJvZHVjdCAxLTE6MTAy"
            }
          }
        }
      ]
    }
  }
}
```
