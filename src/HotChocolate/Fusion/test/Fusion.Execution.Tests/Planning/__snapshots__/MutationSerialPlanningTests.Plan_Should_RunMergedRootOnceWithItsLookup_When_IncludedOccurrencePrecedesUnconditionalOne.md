# Plan_Should_RunMergedRootOnceWithItsLookup_When_IncludedOccurrencePrecedesUnconditionalOne

## Plan

```yaml
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
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Operation
    schema: ledger
    operation: |
      mutation Included_123456789101112_1 {
        a: bump(counter: "included", by: 1) {
          id
        }
      }
    conditions:
      - variable: $x
        passingValue: true
  - id: 2
    type: ApolloOperation
    schema: reader
    operation: |
      query($representations: [_Any!]!) {
        _entities(representations: $representations) {
          ... on Counter {
            value
          }
        }
      }
    source: $._entities
    target: $.a
    requirements:
      - name: __fusion_1_id
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
    operation: |
      mutation Included_123456789101112_3 {
        b: bump(counter: "included", by: 10) {
          id
        }
      }
  - id: 4
    type: ApolloOperation
    schema: reader
    operation: |
      query($representations: [_Any!]!) {
        _entities(representations: $representations) {
          ... on Counter {
            value
          }
        }
      }
    source: $._entities
    target: $.b
    requirements:
      - name: __fusion_2_id
        selectionMap: >-
          id
    dependencies:
      - id: 3
  - id: 5
    type: Operation
    schema: ledger
    operation: |
      mutation Included_123456789101112_5 {
        a: bump(counter: "included", by: 1) {
          id
        }
      }
    conditions:
      - variable: $x
        passingValue: false
  - id: 6
    type: ApolloOperation
    schema: reader
    operation: |
      query($representations: [_Any!]!) {
        _entities(representations: $representations) {
          ... on Counter {
            value
          }
        }
      }
    source: $._entities
    target: $.a
    requirements:
      - name: __fusion_3_id
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
