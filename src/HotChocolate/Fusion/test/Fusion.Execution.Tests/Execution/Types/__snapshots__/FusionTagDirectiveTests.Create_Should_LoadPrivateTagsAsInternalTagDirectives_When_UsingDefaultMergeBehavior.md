# Create_Should_LoadPrivateTagsAsInternalTagDirectives_When_UsingDefaultMergeBehavior

```json
{
  "Definition": {
    "Name": "tag",
    "IsPublic": true,
    "HasFusionTagDefinition": false
  },
  "FieldViews": {
    "Default": {
      "Count": 0,
      "ContainsTag": false,
      "Tags": []
    },
    "WithInternals": [
      {
        "Name": "tag",
        "IsPublic": false,
        "TagName": "a",
        "DefinitionName": "tag",
        "DefinitionIsPublic": true,
        "UsesSchemaDefinition": true
      },
      {
        "Name": "tag",
        "IsPublic": false,
        "TagName": "b",
        "DefinitionName": "tag",
        "DefinitionIsPublic": true,
        "UsesSchemaDefinition": true
      }
    ]
  },
  "Locations": [
    {
      "Name": "Schema",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Object",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Field",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Argument",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Tagged object",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Interface",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Union",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Scalar",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Enum",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Enum value",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Input object",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    },
    {
      "Name": "Input field",
      "PublicCount": 0,
      "PublicTags": [],
      "AllCount": 2,
      "ContainsFusionTag": false,
      "TagsWithInternals": [
        "a",
        "b"
      ]
    }
  ]
}
```
