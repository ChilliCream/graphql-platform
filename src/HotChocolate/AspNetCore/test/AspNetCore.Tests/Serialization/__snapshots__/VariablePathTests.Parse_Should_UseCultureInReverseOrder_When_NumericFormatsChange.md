# Parse_Should_UseCultureInReverseOrder_When_NumericFormatsChange

```json
[
  {
    "Input": ".variables..head.variables.!2..~1.",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "head"
      },
      {
        "Type": "IndexPathSegment",
        "Value": -2
      },
      {
        "Type": "IndexPathSegment",
        "Value": -1
      }
    ],
    "FormatRequests": 3,
    "Exception": null
  },
  {
    "Input": "wrong.head.~1",
    "Segments": [],
    "FormatRequests": 0,
    "Exception": "GraphQLRequestException"
  },
  {
    "Input": "variables",
    "Segments": [],
    "FormatRequests": 0,
    "Exception": "GraphQLRequestException"
  },
  {
    "Input": "variables.variables",
    "Segments": [],
    "FormatRequests": 0,
    "Exception": "InvalidOperationException"
  }
]
```
