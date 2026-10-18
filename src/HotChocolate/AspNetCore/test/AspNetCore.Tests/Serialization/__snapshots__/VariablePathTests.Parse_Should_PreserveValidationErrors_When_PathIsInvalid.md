# Parse_Should_PreserveValidationErrors_When_PathIsInvalid

```json
[
  {
    "Input": null,
    "Exception": "NullReferenceException",
    "Message": "Object reference not set to an instance of an object.",
    "Errors": []
  },
  {
    "Input": "",
    "Exception": "GraphQLRequestException",
    "Message": "Invalid variable path `` in `map`.",
    "Errors": [
      {
        "Message": "Invalid variable path `` in `map`.",
        "Code": "HC0041"
      }
    ]
  },
  {
    "Input": "...",
    "Exception": "GraphQLRequestException",
    "Message": "Invalid variable path `...` in `map`.",
    "Errors": [
      {
        "Message": "Invalid variable path `...` in `map`.",
        "Code": "HC0041"
      }
    ]
  },
  {
    "Input": "variables",
    "Exception": "GraphQLRequestException",
    "Message": "Invalid variable path `variables` in `map`.",
    "Errors": [
      {
        "Message": "Invalid variable path `variables` in `map`.",
        "Code": "HC0041"
      }
    ]
  },
  {
    "Input": "..wrong..",
    "Exception": "GraphQLRequestException",
    "Message": "Invalid variable path `..wrong..` in `map`.",
    "Errors": [
      {
        "Message": "Invalid variable path `..wrong..` in `map`.",
        "Code": "HC0041"
      }
    ]
  },
  {
    "Input": "wrong.file",
    "Exception": "GraphQLRequestException",
    "Message": "The variable path must start with `variables`.",
    "Errors": [
      {
        "Message": "The variable path must start with `variables`.",
        "Code": "HC0042"
      }
    ]
  },
  {
    "Input": "Variables.file",
    "Exception": "GraphQLRequestException",
    "Message": "The variable path must start with `variables`.",
    "Errors": [
      {
        "Message": "The variable path must start with `variables`.",
        "Code": "HC0042"
      }
    ]
  },
  {
    "Input": "variables.1.foo",
    "Exception": "InvalidOperationException",
    "Message": "The first path segment must be a key.",
    "Errors": []
  },
  {
    "Input": "variables.variables",
    "Exception": "InvalidOperationException",
    "Message": "The first path segment must be a key.",
    "Errors": []
  }
]
```
