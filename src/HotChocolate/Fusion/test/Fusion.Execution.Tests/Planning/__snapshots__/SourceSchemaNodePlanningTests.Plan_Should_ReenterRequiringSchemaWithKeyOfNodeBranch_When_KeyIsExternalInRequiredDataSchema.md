# Plan_Should_ReenterRequiringSchemaWithKeyOfNodeBranch_When_KeyIsExternalInRequiredDataSchema

## 00-requiring-field

```yaml
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
    hash: 123456789101112
    searchSpace: 3
    expandedNodes: 10
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: node
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query RequiringField_123456789101112_2 {
        node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation: |
      query RequiringField_123456789101112_3 {
        node(id: "c1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation: |
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
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_1_text
        selectionMap: >-
          text
      - name: __fusion_2_id
        selectionMap: >-
          id
    dependencies:
      - id: 3
      - id: 5
  - id: 5
    type: Operation
    schema: b
    operation: |
      query RequiringField_123456789101112_5($__fusion_3_id: ID!) {
        node(id: $__fusion_3_id) {
          __typename
          ... on Chat {
            text
          }
        }
      }
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        selectionMap: >-
          id
    dependencies:
      - id: 3

```

## 01-key-and-requiring-field

```yaml
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
    hash: 123456789101112
    searchSpace: 3
    expandedNodes: 10
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: node
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query KeyAndRequiringField_123456789101112_2 {
        node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation: |
      query KeyAndRequiringField_123456789101112_3 {
        node(id: "c1") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation: |
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
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_1_text
        selectionMap: >-
          text
      - name: __fusion_2_id
        selectionMap: >-
          id
    dependencies:
      - id: 3
      - id: 5
  - id: 5
    type: Operation
    schema: b
    operation: |
      query KeyAndRequiringField_123456789101112_5($__fusion_3_id: ID!) {
        node(id: $__fusion_3_id) {
          __typename
          ... on Chat {
            text
          }
        }
      }
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        selectionMap: >-
          id
    dependencies:
      - id: 3

```

## 02-field-and-requiring-field

```yaml
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
    hash: 123456789101112
    searchSpace: 3
    expandedNodes: 10
nodes:
  - id: 1
    type: Node
    idValue: "c1"
    responseName: node
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query FieldAndRequiringField_123456789101112_2 {
        node(id: "c1") {
          __typename
        }
      }
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: a
    operation: |
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
    dependencies:
      - id: 1
      - id: 2
  - id: 4
    type: Operation
    schema: a
    operation: |
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
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_1_text
        selectionMap: >-
          text
      - name: __fusion_2_id
        selectionMap: >-
          id
    dependencies:
      - id: 3
      - id: 5
  - id: 5
    type: Operation
    schema: b
    operation: |
      query FieldAndRequiringField_123456789101112_5($__fusion_3_id: ID!) {
        node(id: $__fusion_3_id) {
          __typename
          ... on Chat {
            text
          }
        }
      }
    source: $.node<Chat>
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        selectionMap: >-
          id
    dependencies:
      - id: 3

```
