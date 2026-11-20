# Plan_Should_TakeRequirementLookupKeyFromNodeBranch_When_OnlyRequiringSchemaOwnsKey

## requiring-field

```yaml
id: 4c7b8c57aa372fba1dd07747c1fe2172b4b87246d82669aefc7be241108d4dd6
operation:
  - document: |
      query RequiringField {
        node(id: "Q2hhdDox") {
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
    idValue: "\"Q2hhdDox\""
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
          node(id: "Q2hhdDox") {
            __typename
          }
        }
      hash: c048539a5427d223ca44dbe2f043ae97071eca377566e551a0a4d89eb94730f1
      shortHash: c048539a
      xxHash: 4435584790432283705
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
          node(id: "Q2hhdDox") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: 1f96bd8cd83a507e6dc9ad23197ed32f37dcd452c9a3c6c2465cf50d88605d4f
      shortHash: 1f96bd8c
      xxHash: 16915098278301859189
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
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
id: f2ea30b893ed6e130e862943ca331bf990620561ce4a308e6362f557d4f4f38b
operation:
  - document: |
      query KeyAndRequiringField {
        node(id: "Q2hhdDox") {
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
    idValue: "\"Q2hhdDox\""
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
          node(id: "Q2hhdDox") {
            __typename
          }
        }
      hash: fbbb6d09cecee52810c84613bc6b3de75c74a321c0183da8171eddadd21409aa
      shortHash: fbbb6d09
      xxHash: 691073199251964160
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
          node(id: "Q2hhdDox") {
            __typename
            ... on Chat {
              __typename
              id
            }
          }
        }
      hash: 5dbb04ff0b2e060ac88ba687c1c881ba175609123c500e26129763b8db5dd401
      shortHash: 5dbb04ff
      xxHash: 8464688697158387639
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename id } } }
    dependencies:
      - id: 1
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
id: 711ff1864edbe379d809d17a97dd37bd87f4f3130e0bd453657d6d6cd53b90c1
operation:
  - document: |
      query FieldAndRequiringField {
        node(id: "Q2hhdDox") {
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
    idValue: "\"Q2hhdDox\""
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
          node(id: "Q2hhdDox") {
            __typename
          }
        }
      hash: a91ce58c2c228b5ff964f4090914d7658bb0769c7c0b5ab59a0bcb264a93b0ae
      shortHash: a91ce58c
      xxHash: 3602907814108768594
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
          node(id: "Q2hhdDox") {
            __typename
            ... on Chat {
              __typename
              title
              id
            }
          }
        }
      hash: 885807a31f247862c028c07ce4d6003e521efb054ff07a14f1d775d351b762d5
      shortHash: 885807a3
      xxHash: 11078427216797202291
    lookupTypeName: Chat
    resultSelectionSet: >-
      { node { __typename ... on Chat { __typename title id } } }
    dependencies:
      - id: 1
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
