# Plan_Should_RunMergedRootOnceWithItsLookup_When_IncludedOccurrencePrecedesUnconditionalOne

## Plan

```yaml
id: ed15b73a28c38963c6a47a0d2f21f661ab9bcc6aade2578afa7797e34768a13c
operation:
  - document: |
      mutation Included($x: Boolean!) {
        a: bump(counter: "included", by: 1) @include(if: $x) {
          value
          id @fusion__requirement
        }
        b: bump(counter: "included", by: 10) {
          value
          id @fusion__requirement
        }
        a: bump(counter: "included", by: 1) @skip(if: $x) {
          value
          id @fusion__requirement
        }
      }
    name: Included
    kind: Mutation
    id: "123456789101112"
    hash: "123456789101112"
    shortHash: "123456789101112"
searchSpace: 1
expandedNodes: 1
nodes:
  - id: 1
    type: Operation
    schema: ledger
    operation:
      name: Included_123456789101112_1
      kind: Mutation
      document: |
        mutation Included_123456789101112_1 {
          a: bump(counter: "included", by: 1) {
            id
          }
        }
      hash: 9d335145cc59913534e600fbddf5765b1708fd0a70093b0adcb8af8a61bed97e
      shortHash: 9d335145
      xxHash: 17913717854976013341
    resultSelectionSet: >-
      { a }
    conditions:
      - variable: $x
        passingValue: true
  - id: 2
    type: ApolloOperation
    schema: reader
    operation:
      name: Included_123456789101112_2
      kind: Query
      document: |
        query($representations: [_Any!]!) {
          _entities(representations: $representations) {
            ... on Counter {
              value
            }
          }
        }
      hash: f4a36f39ed26e3d60019b181ab45e4d5ae141ffa1aab7befadfff02c3f9079c9
      shortHash: f4a36f39
      xxHash: 13877674936289671579
    entityType: Counter
    resultSelectionSet: >-
      { value }
    source: $._entities
    target: $.a
    requirements:
      - name: __fusion_1_id
        type: ID!
        path: $.a
        selectionMap: >-
          id
    conditions:
      - variable: $x
        passingValue: true
    dependencies:
      - id: 1
  - id: 3
    type: Operation
    schema: ledger
    operation:
      name: Included_123456789101112_3
      kind: Mutation
      document: |
        mutation Included_123456789101112_3 {
          b: bump(counter: "included", by: 10) {
            id
          }
        }
      hash: 2f00a77759e7090963558dd7ceb82d8b77db5b2318abbcf0444a38269af6ba3b
      shortHash: 2f00a777
      xxHash: 11858002327855761730
    resultSelectionSet: >-
      { b }
  - id: 4
    type: ApolloOperation
    schema: reader
    operation:
      name: Included_123456789101112_4
      kind: Query
      document: |
        query($representations: [_Any!]!) {
          _entities(representations: $representations) {
            ... on Counter {
              value
            }
          }
        }
      hash: f4a36f39ed26e3d60019b181ab45e4d5ae141ffa1aab7befadfff02c3f9079c9
      shortHash: f4a36f39
      xxHash: 13877674936289671579
    entityType: Counter
    resultSelectionSet: >-
      { value }
    source: $._entities
    target: $.b
    requirements:
      - name: __fusion_2_id
        type: ID!
        path: $.b
        selectionMap: >-
          id
    dependencies:
      - id: 3
  - id: 5
    type: Operation
    schema: ledger
    operation:
      name: Included_123456789101112_5
      kind: Mutation
      document: |
        mutation Included_123456789101112_5 {
          a: bump(counter: "included", by: 1) {
            id
          }
        }
      hash: 62eeb58b76dacfd29cf8259f0b8297955e3c8af04cd4709041697b07254499b3
      shortHash: 62eeb58b
      xxHash: 3174045493325375849
    resultSelectionSet: >-
      { a }
    conditions:
      - variable: $x
        passingValue: false
  - id: 6
    type: ApolloOperation
    schema: reader
    operation:
      name: Included_123456789101112_6
      kind: Query
      document: |
        query($representations: [_Any!]!) {
          _entities(representations: $representations) {
            ... on Counter {
              value
            }
          }
        }
      hash: f4a36f39ed26e3d60019b181ab45e4d5ae141ffa1aab7befadfff02c3f9079c9
      shortHash: f4a36f39
      xxHash: 13877674936289671579
    entityType: Counter
    resultSelectionSet: >-
      { value }
    source: $._entities
    target: $.a
    requirements:
      - name: __fusion_3_id
        type: ID!
        path: $.a
        selectionMap: >-
          id
    conditions:
      - variable: $x
        passingValue: false
    dependencies:
      - id: 5

```

## Root order when $x: true

```text
a (1), b (3)
```

## Root order when $x: false

```text
b (3), a (5)
```
