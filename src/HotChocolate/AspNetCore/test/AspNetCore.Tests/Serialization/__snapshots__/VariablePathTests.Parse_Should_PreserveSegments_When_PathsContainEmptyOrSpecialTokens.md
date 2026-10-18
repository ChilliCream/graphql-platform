# Parse_Should_PreserveSegments_When_PathsContainEmptyOrSpecialTokens

```json
[
  {
    "Input": "variables.upload",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "upload"
      }
    ]
  },
  {
    "Input": ".variables..input.0.0.file.",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "input"
      },
      {
        "Type": "IndexPathSegment",
        "Value": 0
      },
      {
        "Type": "IndexPathSegment",
        "Value": 0
      },
      {
        "Type": "KeyPathSegment",
        "Value": "file"
      }
    ]
  },
  {
    "Input": "variables.foo.variables.01.bar",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "foo"
      },
      {
        "Type": "IndexPathSegment",
        "Value": 1
      },
      {
        "Type": "KeyPathSegment",
        "Value": "bar"
      }
    ]
  },
  {
    "Input": "variables. file ",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": " file "
      }
    ]
  },
  {
    "Input": "variables.files. +1 ",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "files"
      },
      {
        "Type": "IndexPathSegment",
        "Value": 1
      }
    ]
  },
  {
    "Input": "variables.files.2147483648",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "files"
      },
      {
        "Type": "KeyPathSegment",
        "Value": "2147483648"
      }
    ]
  },
  {
    "Input": "variables.2147483648",
    "Segments": [
      {
        "Type": "KeyPathSegment",
        "Value": "2147483648"
      }
    ]
  }
]
```
