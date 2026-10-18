# Parse_Should_PreserveFileBindings_When_MapsContainOrdinaryUploadPaths

```json
[
  {
    "Name": "SingleFile",
    "RootFileKey": null,
    "Bindings": [
      {
        "Nodes": [
          {
            "Key": "file",
            "FileKey": "0"
          }
        ],
        "FileKey": "0"
      }
    ],
    "Missing": false,
    "WrongCase": false,
    "Prefix": false
  },
  {
    "Name": "List2",
    "RootFileKey": null,
    "Bindings": [
      {
        "Nodes": [
          {
            "Key": "files",
            "FileKey": null
          },
          {
            "Key": "0",
            "FileKey": "0"
          }
        ],
        "FileKey": "0"
      },
      {
        "Nodes": [
          {
            "Key": "files",
            "FileKey": null
          },
          {
            "Key": "1",
            "FileKey": "1"
          }
        ],
        "FileKey": "1"
      }
    ],
    "Missing": false,
    "WrongCase": false,
    "Prefix": false
  },
  {
    "Name": "NestedList2",
    "RootFileKey": null,
    "Bindings": [
      {
        "Nodes": [
          {
            "Key": "input",
            "FileKey": null
          },
          {
            "Key": "files",
            "FileKey": null
          },
          {
            "Key": "0",
            "FileKey": "0"
          }
        ],
        "FileKey": "0"
      },
      {
        "Nodes": [
          {
            "Key": "input",
            "FileKey": null
          },
          {
            "Key": "files",
            "FileKey": null
          },
          {
            "Key": "1",
            "FileKey": "1"
          }
        ],
        "FileKey": "1"
      }
    ],
    "Missing": false,
    "WrongCase": false,
    "Prefix": false
  },
  {
    "Name": "EmptyMapControl",
    "RootFileKey": null,
    "Bindings": [],
    "Missing": false,
    "WrongCase": false,
    "Prefix": false
  }
]
```
