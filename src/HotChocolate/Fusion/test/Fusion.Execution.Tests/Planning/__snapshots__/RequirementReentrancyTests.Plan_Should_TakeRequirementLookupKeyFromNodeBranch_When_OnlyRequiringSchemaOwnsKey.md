# Plan_Should_TakeRequirementLookupKeyFromNodeBranch_When_OnlyRequiringSchemaOwnsKey

## requiring-field

```yaml
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
    hash: 123456789101112
    searchSpace: 2
    expandedNodes: 6
nodes:
  - id: 1
    type: Node
    idValue: "Q2hhdDox"
    responseName: node
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query RequiringField_123456789101112_2 {
        node(id: "Q2hhdDox") {
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
        node(id: "Q2hhdDox") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
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
        chatById(id: $__fusion_3_id) {
          text
        }
      }
    source: $.chatById
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        selectionMap: >-
          id
    dependencies:
      - id: 3

```

## key-and-requiring-field

```yaml
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
    hash: 123456789101112
    searchSpace: 2
    expandedNodes: 6
nodes:
  - id: 1
    type: Node
    idValue: "Q2hhdDox"
    responseName: node
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query KeyAndRequiringField_123456789101112_2 {
        node(id: "Q2hhdDox") {
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
        node(id: "Q2hhdDox") {
          __typename
          ... on Chat {
            __typename
            id
          }
        }
      }
    dependencies:
      - id: 1
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
        chatById(id: $__fusion_3_id) {
          text
        }
      }
    source: $.chatById
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        selectionMap: >-
          id
    dependencies:
      - id: 3

```

## field-and-requiring-field

```yaml
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
    hash: 123456789101112
    searchSpace: 2
    expandedNodes: 6
nodes:
  - id: 1
    type: Node
    idValue: "Q2hhdDox"
    responseName: node
    branches:
      - Chat: 3
    fallback: 2
  - id: 2
    type: Operation
    operation: |
      query FieldAndRequiringField_123456789101112_2 {
        node(id: "Q2hhdDox") {
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
        node(id: "Q2hhdDox") {
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
        chatById(id: $__fusion_3_id) {
          text
        }
      }
    source: $.chatById
    target: $.node<Chat>
    requirements:
      - name: __fusion_3_id
        selectionMap: >-
          id
    dependencies:
      - id: 3

```
