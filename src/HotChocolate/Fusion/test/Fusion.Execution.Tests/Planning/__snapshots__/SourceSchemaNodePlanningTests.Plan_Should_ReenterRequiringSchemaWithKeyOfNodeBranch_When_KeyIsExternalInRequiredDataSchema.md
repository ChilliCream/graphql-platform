# Plan_Should_ReenterRequiringSchemaWithKeyOfNodeBranch_When_KeyIsExternalInRequiredDataSchema

## 00-requiring-field

```yaml
id: 42e145970f4f02d613785559887f48b9d7fc5f4641dcdd80d10e66d08b0812ad
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
searchSpace: 3
expandedNodes: 10
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
          node(id: $__fusion_3_id) {
            __typename
            ... on Chat {
              text
            }
          }
        }
      hash: 68aeecbb994aa9cb4f0679e83dc776f0d5fe1ed4a1df4d80b3f520ea1d67f2de
      shortHash: 68aeecbb
      xxHash: 13272904528386571212
    lookupTypeName: Chat
    resultSelectionSet: >-
      { text }
    source: $.node<Chat>
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

## 01-key-and-requiring-field

```yaml
id: 71acea306a44ac88eee210aaf94e70502b5bfd877c98f7f98f3e5e2ad765b60a
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
searchSpace: 3
expandedNodes: 10
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
          node(id: $__fusion_3_id) {
            __typename
            ... on Chat {
              text
            }
          }
        }
      hash: 144a2713a9c0d4b73fde387ace26147492061520dc6e91b5ced2c6ea8bdbcac2
      shortHash: 144a2713
      xxHash: 13364533571640477865
    lookupTypeName: Chat
    resultSelectionSet: >-
      { text }
    source: $.node<Chat>
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

## 02-field-and-requiring-field

```yaml
id: 528905d254db78ce519db49825191f9577e6959c6901f32802fb346624337d5e
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
searchSpace: 3
expandedNodes: 10
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
          node(id: $__fusion_3_id) {
            __typename
            ... on Chat {
              text
            }
          }
        }
      hash: ebeb9c80df7f09df9c4df82699bdbfb96a9023fa960bafeb2b19e6367e52faed
      shortHash: ebeb9c80
      xxHash: 13007722545655834565
    lookupTypeName: Chat
    resultSelectionSet: >-
      { text }
    source: $.node<Chat>
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
