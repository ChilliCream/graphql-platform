# Plan_Should_OrderRootsByFirstIncludedOccurrence_When_ResponseNameRepeatsUnderConditions

## Plan

```yaml
operation:
  - document: |
      mutation Conditioned($counter: ID!, $a: Boolean!, $b: Boolean!) {
        first: bump(counter: $counter, by: 1) @include(if: $a)
        second: add(counter: $counter, by: 10) @include(if: $b)
        first: bump(counter: $counter, by: 1) @skip(if: $a) @include(if: $b)
        last: bump(counter: $counter, by: 100)
      }
    name: Conditioned
    hash: 123456789101112
    searchSpace: 1
    expandedNodes: 1
nodes:
  - id: 1
    type: Operation
    schema: catalog
    operation: |
      mutation Conditioned_123456789101112_1($counter: ID!) {
        first: bump(counter: $counter, by: 1)
      }
    conditions:
      - variable: $a
        passingValue: true
    forwardedVariables:
      - counter
  - id: 2
    type: Operation
    schema: details
    operation: |
      mutation Conditioned_123456789101112_2($counter: ID!) {
        second: add(counter: $counter, by: 10)
      }
    conditions:
      - variable: $b
        passingValue: true
    forwardedVariables:
      - counter
  - id: 3
    type: Operation
    schema: catalog
    operation: |
      mutation Conditioned_123456789101112_3($counter: ID!) {
        first: bump(counter: $counter, by: 1)
      }
    conditions:
      - variable: $a
        passingValue: false
      - variable: $b
        passingValue: true
    forwardedVariables:
      - counter
  - id: 4
    type: Operation
    schema: catalog
    operation: |
      mutation Conditioned_123456789101112_4($counter: ID!) {
        last: bump(counter: $counter, by: 100)
      }
    forwardedVariables:
      - counter

```

## Root order when $a: true, $b: true

```text
first (1), second (2), last (4)
```

## Root order when $a: true, $b: false

```text
first (1), last (4)
```

## Root order when $a: false, $b: true

```text
second (2), first (3), last (4)
```

## Root order when $a: false, $b: false

```text
last (4)
```
