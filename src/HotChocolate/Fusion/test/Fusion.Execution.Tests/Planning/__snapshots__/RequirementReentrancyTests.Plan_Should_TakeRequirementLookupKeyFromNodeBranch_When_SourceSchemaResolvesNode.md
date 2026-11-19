# Plan_Should_TakeRequirementLookupKeyFromNodeBranch_When_SourceSchemaResolvesNode

## requiring-field

```yaml
id: dc9f51d2b38afe314630e25105202e45f7bc156517cc480b0c6169ce50f07e06
operation:
  - document: |
      query RequiringField {
        node(id: "c1") {
          __typename @fusion__requirement
          ... on Chat {
            summary
            id @fusion__requirement
            text @fusion__requirement
          }
        }
      }
    name: RequiringField
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 2
expandedNodes: 6
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: node
    branches:
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: RequiringField_123456789101112_2
      kind: Query
      document: |
        query RequiringField_123456789101112_2 {
          node(id: "c1") {
            __typename
          }
        }
      hash: d68e1e1e12ea9c360dba2cf9580b7fcf6456cf51a49aa44c0c5786371f08ecbb
      shortHash: d68e1e1e
      xxHash: 15267356908629616184
    resultSelectionSet: >-
      { node }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation:
      name: RequiringField_123456789101112_3
      kind: Query
      document: |
        query RequiringField_123456789101112_3 {
          node(id: "c1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: 252dee94284dd866d042f8d28f08fadb82e17be2f5ead5c880e957ba0ab633b9
      shortHash: 252dee94
      xxHash: 761144002767513794
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation:
      name: RequiringField_123456789101112_4
      kind: Query
      document: |
        query RequiringField_123456789101112_4(
          $__fusion_1_text: String
          $__fusion_2_id: ID!
        ) {
          node(id: $__fusion_2_id) {
            __typename
            ... on Chat {
              summary(text: $__fusion_1_text)
            }
          }
        }
      hash: 959c45f8516558fe0941f8e944fb8c989224c7b6d182848f137d8dc6654512e5
      shortHash: 959c45f8
      xxHash: 1973436499700985632
    lookupTypeName: Chat
    resultSelectionSet: >-
      { summary }
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_1_text
        type: String
        path: $.node<Chat>
        selectionMap: >-
          text
      - name: __fusion_2_id
        type: ID!
        path: $.node<Chat>
        selectionMap: >-
          id
    dependencies:
      - id: 3
      - id: 5
  - id: 5
    type: Operation
    schema: b
    operation:
      name: RequiringField_123456789101112_5
      kind: Query
      document: |
        query RequiringField_123456789101112_5($__fusion_3_id: ID!) {
          chatById(id: $__fusion_3_id) {
            text
          }
        }
      hash: 66dc9a57da318b4b07a92dedd533b959094968592ef1e08044307362a44bc372
      shortHash: 66dc9a57
      xxHash: 17963717171088492158
    lookupTypeName: Chat
    resultSelectionSet: >-
      { text }
    source: $.chatById
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        type: ID!
        path: $.node<Chat>
        selectionMap: >-
          id
    dependencies:
      - id: 3

```

## key-and-requiring-field

```yaml
id: 9c50221a318b84c6d4dd24e0dde393c3df44f3f11b7c7629ccd8049842b3b291
operation:
  - document: |
      query KeyAndRequiringField {
        node(id: "c1") {
          __typename @fusion__requirement
          ... on Chat {
            id
            id @fusion__requirement
            summary
            text @fusion__requirement
          }
        }
      }
    name: KeyAndRequiringField
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 2
expandedNodes: 6
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: node
    branches:
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: KeyAndRequiringField_123456789101112_2
      kind: Query
      document: |
        query KeyAndRequiringField_123456789101112_2 {
          node(id: "c1") {
            __typename
          }
        }
      hash: ce1b849b902f3f40973add9477f3d059447a050b8d5871619937a9e76f11da2e
      shortHash: ce1b849b
      xxHash: 5698955003726083366
    resultSelectionSet: >-
      { node }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation:
      name: KeyAndRequiringField_123456789101112_3
      kind: Query
      document: |
        query KeyAndRequiringField_123456789101112_3 {
          node(id: "c1") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: 9fbc920941b6c6a1ba25be2fe132e6cc2a370ec6ebc5c7cb6ffb2a8bcd59e4ff
      shortHash: 9fbc9209
      xxHash: 8451177817706333790
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation:
      name: KeyAndRequiringField_123456789101112_4
      kind: Query
      document: |
        query KeyAndRequiringField_123456789101112_4(
          $__fusion_1_text: String
          $__fusion_2_id: ID!
        ) {
          node(id: $__fusion_2_id) {
            __typename
            ... on Chat {
              summary(text: $__fusion_1_text)
            }
          }
        }
      hash: 46095a834b1ab57a60232004c3f2e702510847a47c9a22b345339cdda4d7ad67
      shortHash: 46095a83
      xxHash: 17890486572813477537
    lookupTypeName: Chat
    resultSelectionSet: >-
      { summary }
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_1_text
        type: String
        path: $.node<Chat>
        selectionMap: >-
          text
      - name: __fusion_2_id
        type: ID!
        path: $.node<Chat>
        selectionMap: >-
          id
    dependencies:
      - id: 3
      - id: 5
  - id: 5
    type: Operation
    schema: b
    operation:
      name: KeyAndRequiringField_123456789101112_5
      kind: Query
      document: |
        query KeyAndRequiringField_123456789101112_5($__fusion_3_id: ID!) {
          chatById(id: $__fusion_3_id) {
            text
          }
        }
      hash: 21c1a461aae1f0aac3806191a5ddd20a2624974f32b356cdef60d5a22ae1e8a2
      shortHash: 21c1a461
      xxHash: 11240390208754655118
    lookupTypeName: Chat
    resultSelectionSet: >-
      { text }
    source: $.chatById
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        type: ID!
        path: $.node<Chat>
        selectionMap: >-
          id
    dependencies:
      - id: 3

```

## field-and-requiring-field

```yaml
id: 81e533251ddeaaeffc483662bbcdad8b06c2c2d6cfe188b2c2c63168a34006a0
operation:
  - document: |
      query FieldAndRequiringField {
        node(id: "c1") {
          __typename @fusion__requirement
          ... on Chat {
            title
            summary
            id @fusion__requirement
            text @fusion__requirement
          }
        }
      }
    name: FieldAndRequiringField
    kind: Query
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 2
expandedNodes: 6
nodes:
  - id: 1
    type: Node
    idValue: "\"c1\""
    responseName: node
    branches:
      Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation:
      name: FieldAndRequiringField_123456789101112_2
      kind: Query
      document: |
        query FieldAndRequiringField_123456789101112_2 {
          node(id: "c1") {
            __typename
          }
        }
      hash: 8bccd62d4267ef15c56e63b51b4e6b6bd347bf793a4d5975c5a512b4cd235144
      shortHash: 8bccd62d
      xxHash: 17288866814480803768
    resultSelectionSet: >-
      { node }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation:
      name: FieldAndRequiringField_123456789101112_3
      kind: Query
      document: |
        query FieldAndRequiringField_123456789101112_3 {
          node(id: "c1") {
            __typename
            ... on Chat {
              __typename
              title
              id
            }
          }
        }
      hash: d3c97c562527608f35127f50fd9c6d9997646afd35477930b1deaa1f5a482e08
      shortHash: d3c97c56
      xxHash: 6331603947203779120
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename title id } } }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation:
      name: FieldAndRequiringField_123456789101112_4
      kind: Query
      document: |
        query FieldAndRequiringField_123456789101112_4(
          $__fusion_1_text: String
          $__fusion_2_id: ID!
        ) {
          node(id: $__fusion_2_id) {
            __typename
            ... on Chat {
              summary(text: $__fusion_1_text)
            }
          }
        }
      hash: 1daa14045af2a6d1acde917d9ee33ee6a18c2446bf0165fa1a6a0c035760e7a2
      shortHash: 1daa1404
      xxHash: 2389783063741167640
    lookupTypeName: Chat
    resultSelectionSet: >-
      { summary }
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_1_text
        type: String
        path: $.node<Chat>
        selectionMap: >-
          text
      - name: __fusion_2_id
        type: ID!
        path: $.node<Chat>
        selectionMap: >-
          id
    dependencies:
      - id: 3
      - id: 5
  - id: 5
    type: Operation
    schema: b
    operation:
      name: FieldAndRequiringField_123456789101112_5
      kind: Query
      document: |
        query FieldAndRequiringField_123456789101112_5($__fusion_3_id: ID!) {
          chatById(id: $__fusion_3_id) {
            text
          }
        }
      hash: a0fd029ade4dde3300523f1c58448ee05fd89413fb117cc98d3014b0facd4787
      shortHash: a0fd029a
      xxHash: 1916713148820417483
    lookupTypeName: Chat
    resultSelectionSet: >-
      { text }
    source: $.chatById
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        type: ID!
        path: $.node<Chat>
        selectionMap: >-
          id
    dependencies:
      - id: 3

```
